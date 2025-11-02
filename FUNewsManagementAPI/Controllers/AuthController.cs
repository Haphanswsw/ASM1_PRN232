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

    // ✅ ADD: Response model đầy đủ cho /me
    public sealed class MeResponse
    {
        public short AccountId { get; set; }
        public string? AccountName { get; set; }
        public string? AccountEmail { get; set; }
        public int? AccountRole { get; set; }
        public string RoleName { get; set; } = default!;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        Console.WriteLine($"[LOGIN] Attempt for: {req.Email}");

        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        {
            Console.WriteLine($"[LOGIN] Empty credentials");
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var account = await _db.SystemAccounts
            .FirstOrDefaultAsync(a => a.AccountEmail == req.Email && a.AccountPassword == req.Password);

        if (account == null)
        {
            Console.WriteLine($"[LOGIN] Account not found: {req.Email}");
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var roleName = MapRoleName(account.AccountRole);
        var (token, expiresAtUtc) = GenerateJwt(account, roleName);

        Console.WriteLine($"[LOGIN] ✅ Success - AccountId: {account.AccountId}, Role: {roleName}");

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
        return Ok(new { message = "Logged out" });
    }

    // ✅ FIX: Trả về đầy đủ thông tin từ database, không chỉ claims
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        Console.WriteLine($"[GET ME] Authenticated: {User.Identity?.IsAuthenticated}");

        if (!User.Identity?.IsAuthenticated ?? true)
        {
            Console.WriteLine($"[GET ME] Not authenticated");
            return Unauthorized();
        }

        // Lấy AccountId từ JWT token
        var accountIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine($"[GET ME] AccountId from JWT: {accountIdStr}");

        if (string.IsNullOrEmpty(accountIdStr))
        {
            Console.WriteLine($"[GET ME] No AccountId in token");
            return Unauthorized(new { message = "Invalid token: missing account ID" });
        }

        if (!short.TryParse(accountIdStr, out var accountId))
        {
            Console.WriteLine($"[GET ME] Invalid AccountId format: {accountIdStr}");
            return BadRequest(new { message = "Invalid account ID format" });
        }

        // ✅ LẤY THÔNG TIN ĐẦY ĐỦ TỪ DATABASE Dựa vào AccountId trong JWT
        var account = await _db.SystemAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountId == accountId);

        if (account == null)
        {
            Console.WriteLine($"[GET ME] Account {accountId} not found in database");
            return NotFound(new { message = "Account not found" });
        }

        var roleName = MapRoleName(account.AccountRole);
        Console.WriteLine($"[GET ME] ✅ Found - Email: {account.AccountEmail}, Role: {roleName}");

        // ✅ Trả về đầy đủ thông tin
        return Ok(new MeResponse
        {
            AccountId = account.AccountId,
            AccountName = account.AccountName,
            AccountEmail = account.AccountEmail,
            AccountRole = account.AccountRole,
            RoleName = roleName
        });
    }

    // ✅ FIX: Đảm bảo chỉ update account của user hiện tại
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest req)
    {
        Console.WriteLine($"[UPDATE ME] Request received");

        // Lấy AccountId từ JWT token
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine($"[UPDATE ME] AccountId from JWT: {idStr}");

        if (string.IsNullOrEmpty(idStr))
        {
            Console.WriteLine($"[UPDATE ME] No AccountId in token");
            return Unauthorized(new { message = "Invalid token" });
        }

        if (!short.TryParse(idStr, out var id))
        {
            Console.WriteLine($"[UPDATE ME] Invalid AccountId format: {idStr}");
            return BadRequest(new { message = "Invalid account ID format" });
        }

        // ✅ TÌM ACCOUNT CỦA USER HIỆN TẠI trong database
        var me = await _db.SystemAccounts.FirstOrDefaultAsync(a => a.AccountId == id);
        if (me == null)
        {
            Console.WriteLine($"[UPDATE ME] Account {id} not found");
            return NotFound(new { message = $"Account with ID {id} not found" });
        }

        Console.WriteLine($"[UPDATE ME] Current account - Email: {me.AccountEmail}, Role: {me.AccountRole}");

        // ✅ Cập nhật thông tin
        bool hasChanges = false;

        if (!string.IsNullOrWhiteSpace(req.Name))
        {
            Console.WriteLine($"[UPDATE ME] Updating name: '{me.AccountName}' -> '{req.Name}'");
            me.AccountName = req.Name;
            hasChanges = true;
        }

        if (!string.IsNullOrWhiteSpace(req.Password))
        {
            Console.WriteLine($"[UPDATE ME] Updating password for account {id}");
            me.AccountPassword = req.Password;
            hasChanges = true;
        }

        if (!hasChanges)
        {
            Console.WriteLine($"[UPDATE ME] No changes to save");
            return BadRequest(new { message = "No fields to update" });
        }

        await _db.SaveChangesAsync();
        Console.WriteLine($"[UPDATE ME] ✅ Successfully updated AccountId: {id}");

        return Ok(new
        {
            message = "Profile updated successfully",
            accountId = me.AccountId,
            accountName = me.AccountName,
            accountEmail = me.AccountEmail
        });
    }

    private string MapRoleName(int? roleVal)
    {
        if (roleVal == _roleSettings.AdminRoleValue) return "Admin";
        return roleVal switch
        {
            1 => "Staff",
            2 => "Lecturer",
            _ => "Lecturer"
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