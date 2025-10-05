using BusinessObjects.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace FUNewsManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly FunewsManagementContext _db;
    private readonly RoleSettings _roleSettings;

    public AuthController(FunewsManagementContext db, IOptions<RoleSettings> roleSettings)
    {
        _db = db;
        _roleSettings = roleSettings.Value;
    }

    public sealed class LoginRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
    public sealed class UpdateMeRequest
    {
        public string? Name { get; set; }
        public string? Password { get; set; }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Unauthorized(new { message = "Invalid credentials." });

        var account = await _db.SystemAccounts
            .FirstOrDefaultAsync(a => a.AccountEmail == req.Email && a.AccountPassword == req.Password);

        if (account == null)
            return Unauthorized(new { message = "Invalid credentials." });

        var roleName = MapRoleName(account.AccountRole);    

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
            new Claim(ClaimTypes.Name, account.AccountName ?? account.AccountEmail ?? string.Empty),
            new Claim(ClaimTypes.Email, account.AccountEmail ?? string.Empty),
            new Claim(ClaimTypes.Role, roleName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Ok(new { message = "Logged in", role = roleName, accountId = account.AccountId });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Logged out" });
    }

    [HttpGet("me")]
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
}