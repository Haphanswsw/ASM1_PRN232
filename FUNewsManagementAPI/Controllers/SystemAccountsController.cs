using BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repositories.Repositories;
using Microsoft.AspNetCore.Identity;

namespace FUNewsManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SystemAccountsController : ControllerBase
{
    private readonly ISystemAccountRepository _repo;
    private readonly FunewsManagementContext _db;

    public SystemAccountsController(ISystemAccountRepository repo, FunewsManagementContext db)
    {
        _repo = repo;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SystemAccount>>> GetAll()
    {
        var items = await _repo.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SystemAccount>> GetById(short id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<SystemAccount>> Create([FromBody] SystemAccount dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.AccountEmail) || string.IsNullOrWhiteSpace(dto.AccountPassword))
            return BadRequest("Invalid payload.");

        var exists = await _db.SystemAccounts.AnyAsync(a => a.AccountId == dto.AccountId || a.AccountEmail == dto.AccountEmail);
        if (exists) return Conflict(new { message = "Account with same ID or Email already exists." });

        var entity = new SystemAccount
        {
            AccountId = dto.AccountId,
            AccountName = dto.AccountName,
            AccountEmail = dto.AccountEmail,
            AccountRole = dto.AccountRole
        };

        var hasher = new PasswordHasher<SystemAccount>();
        entity.AccountPassword = hasher.HashPassword(entity, dto.AccountPassword);

        await _repo.AddAsync(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.AccountId }, entity);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(short id, [FromBody] SystemAccount dto)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        existing.AccountName = dto.AccountName ?? existing.AccountName;
        existing.AccountEmail = dto.AccountEmail ?? existing.AccountEmail;
        existing.AccountRole = dto.AccountRole ?? existing.AccountRole;

        if (!string.IsNullOrWhiteSpace(dto.AccountPassword))
        {
            var hasher = new PasswordHasher<SystemAccount>();
            existing.AccountPassword = hasher.HashPassword(existing, dto.AccountPassword);
        }

        await _repo.UpdateAsync(existing);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(short id)
    {
        var hasArticles = await _db.NewsArticles.AnyAsync(n => n.CreatedById == id);
        if (hasArticles) return Conflict(new { message = "Cannot delete account: it has created one or more news articles." });

        await _repo.DeleteAsync(id);
        return NoContent();
    }
}