using BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly FunewsManagementContext _db;

    public TagsController(FunewsManagementContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<IEnumerable<Tag>>> GetAll()
    {
        var items = await _db.Tags.OrderBy(t => t.TagName).ToListAsync();
        return Ok(items);
    }
}