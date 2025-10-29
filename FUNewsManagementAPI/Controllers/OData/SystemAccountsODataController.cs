using BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementAPI.Controllers.OData;

[Route("odata/SystemAccounts")]
[Authorize(Roles = "Admin")]
public class SystemAccountsODataController : ODataController
{
    private readonly FunewsManagementContext _db;
    public SystemAccountsODataController(FunewsManagementContext db) => _db = db;

    [HttpGet]
    [EnableQuery(PageSize = 50)]
    public IActionResult Get() => Ok(_db.SystemAccounts.AsNoTracking());

    [HttpGet("({key})")]
    [EnableQuery]
    public async Task<IActionResult> Get([FromRoute] short key)
    {
        var item = await _db.SystemAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.AccountId == key);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] SystemAccount entity)
    {
        var exists = await _db.SystemAccounts.AnyAsync(a => a.AccountId == entity.AccountId || a.AccountEmail == entity.AccountEmail);
        if (exists) return Conflict(new { message = "Account with same ID or Email already exists." });

        _db.SystemAccounts.Add(entity);
        await _db.SaveChangesAsync();
        return Created(entity);
    }

    [HttpPut("({key})")]
    public async Task<IActionResult> Put([FromRoute] short key, [FromBody] SystemAccount update)
    {
        var existing = await _db.SystemAccounts.FirstOrDefaultAsync(a => a.AccountId == key);
        if (existing == null) return NotFound();

        existing.AccountName = update.AccountName;
        existing.AccountEmail = update.AccountEmail;
        existing.AccountRole = update.AccountRole;
        existing.AccountPassword = update.AccountPassword;

        await _db.SaveChangesAsync();
        return Updated(existing);
    }

    [HttpPatch("({key})")]
    public async Task<IActionResult> Patch([FromRoute] short key, [FromBody] Delta<SystemAccount> patch)
    {
        var existing = await _db.SystemAccounts.FirstOrDefaultAsync(a => a.AccountId == key);
        if (existing == null) return NotFound();

        patch.Patch(existing);
        await _db.SaveChangesAsync();
        return Updated(existing);
    }

    [HttpDelete("({key})")]
    public async Task<IActionResult> Delete([FromRoute] short key)
    {
        var hasArticles = await _db.NewsArticles.AnyAsync(n => n.CreatedById == key);
        if (hasArticles) return Conflict(new { message = "Cannot delete account: it has created one or more news articles." });

        var existing = await _db.SystemAccounts.FindAsync(key);
        if (existing == null) return NotFound();

        _db.SystemAccounts.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}