using BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repositories.Repositories;
using Microsoft.AspNetCore.OData.Query;

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
    [EnableQuery(PageSize = 50)]
    public IActionResult GetAll()
    {
        var query = _db.SystemAccounts.AsNoTracking();
        return Ok(query);
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
            AccountRole = dto.AccountRole,
            AccountPassword = dto.AccountPassword
        };

        await _repo.AddAsync(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.AccountId }, entity);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(short id, [FromBody] SystemAccount dto)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.AccountName))
            existing.AccountName = dto.AccountName;

        if (!string.IsNullOrWhiteSpace(dto.AccountEmail))
            existing.AccountEmail = dto.AccountEmail;

        if (dto.AccountRole.HasValue)
            existing.AccountRole = dto.AccountRole;

        if (!string.IsNullOrWhiteSpace(dto.AccountPassword))
        {
            existing.AccountPassword = dto.AccountPassword;
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