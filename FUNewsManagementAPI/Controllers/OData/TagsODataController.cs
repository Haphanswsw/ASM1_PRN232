using BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementAPI.Controllers.OData;

[Route("odata/Tags")]
public class TagsODataController : ODataController
{
    private readonly FunewsManagementContext _db;
    public TagsODataController(FunewsManagementContext db) => _db = db;

    [HttpGet]
    [AllowAnonymous]
    [EnableQuery(PageSize = 50)]
    public IActionResult Get() => Ok(_db.Tags.AsNoTracking());

    [HttpGet("({key})")]
    [AllowAnonymous]
    [EnableQuery]
    public async Task<IActionResult> Get([FromRoute] int key)
    {
        var item = await _db.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.TagId == key);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> Post([FromBody] Tag entity)
    {
        var exists = await _db.Tags.AnyAsync(t => t.TagId == entity.TagId);
        if (exists) return Conflict(new { message = "Tag with same ID already exists." });

        _db.Tags.Add(entity);
        await _db.SaveChangesAsync();
        return Created(entity);
    }

    [HttpPut("({key})")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> Put([FromRoute] int key, [FromBody] Tag update)
    {
        var existing = await _db.Tags.FirstOrDefaultAsync(t => t.TagId == key);
        if (existing == null) return NotFound();

        existing.TagName = update.TagName;
        existing.Note = update.Note;

        await _db.SaveChangesAsync();
        return Updated(existing);
    }

    [HttpPatch("({key})")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> Patch([FromRoute] int key, [FromBody] Delta<Tag> patch)
    {
        var existing = await _db.Tags.FirstOrDefaultAsync(t => t.TagId == key);
        if (existing == null) return NotFound();

        patch.Patch(existing);
        await _db.SaveChangesAsync();
        return Updated(existing);
    }

    [HttpDelete("({key})")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> Delete([FromRoute] int key)
    {
        var existing = await _db.Tags.FindAsync(key);
        if (existing == null) return NotFound();

        _db.Tags.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}