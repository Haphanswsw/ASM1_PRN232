using BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FUNewsManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly FunewsManagementContext _db;
    private readonly RoleSettings _roleSettings;
    private readonly IConfiguration _config;

    public AuthController(FunewsManagementContext db, IOptions<RoleSettings> roleSettings, IConfiguration config)
    {
        _db = db;
        _roleSettings = roleSettings.Value;
        _config = config;
    }

    public sealed class LoginRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
    public sealed class LoginResponse
    {
        public string Token { get; set; } = default!;
        public DateTime ExpiresAtUtc { get; set; }
        public string Role { get; set; } = default!;
        public short AccountId { get; set; }
    }
    public sealed class UpdateMeRequest
    {
        public string? Name { get; set; }
        public string? Password { get; set; }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Unauthorized(new { message = "Invalid credentials." });

        var account = await _db.SystemAccounts
            .FirstOrDefaultAsync(a => a.AccountEmail == req.Email && a.AccountPassword == req.Password);

        if (account == null)
            return Unauthorized(new { message = "Invalid credentials." });

        var roleName = MapRoleName(account.AccountRole);
        var (token, expiresAtUtc) = GenerateJwt(account, roleName);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            Role = roleName,
            AccountId = account.AccountId
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Stateless JWT: client just discards the token.
        return Ok(new { message = "Logged out" });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized();

        return Ok(new
        {
            accountId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            name = User.Identity?.Name,
            email = User.FindFirstValue(ClaimTypes.Email),
            role = User.FindFirstValue(ClaimTypes.Role)
        });
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest req)
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(idStr)) return Unauthorized();
        var id = short.Parse(idStr);

        var me = await _db.SystemAccounts.FirstOrDefaultAsync(a => a.AccountId == id);
        if (me == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(req.Name)) me.AccountName = req.Name;
        if (!string.IsNullOrWhiteSpace(req.Password)) me.AccountPassword = req.Password;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private string MapRoleName(int? roleVal)
    {
        if (roleVal == _roleSettings.AdminRoleValue) return "Admin";
        return roleVal switch
        {
            1 => "Staff",
            2 => "Lecturer",
            _ => "Lecturer" // default to lowest privilege
        };
    }

    private (string token, DateTime expiresAtUtc) GenerateJwt(SystemAccount account, string roleName)
    {
        var jwtSection = _config.GetSection("Authentication:Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var key = jwtSection["Key"]!;
        var expiresInMinutes = int.TryParse(jwtSection["ExpiresInMinutes"], out var m) ? m : 120;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
            new Claim(ClaimTypes.Name, account.AccountName ?? account.AccountEmail ?? string.Empty),
            new Claim(ClaimTypes.Email, account.AccountEmail ?? string.Empty),
            new Claim(ClaimTypes.Role, roleName)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiresInMinutes);

        var token = new JwtSecurityToken(
            issuer: string.IsNullOrWhiteSpace(issuer) ? null : issuer,
            audience: string.IsNullOrWhiteSpace(audience) ? null : audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}