using BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Query; // added

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
    [EnableQuery(PageSize = 50)]
    public IActionResult GetAll()
    {
        var query = _db.Tags.AsNoTracking().OrderBy(t => t.TagName);
        return Ok(query);
    }
}