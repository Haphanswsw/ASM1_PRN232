using System.ComponentModel.DataAnnotations;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repositories.Repositories;
using Microsoft.AspNetCore.OData.Query;

namespace FUNewsManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Staff,Admin")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _repo;
    private readonly FunewsManagementContext _db;

    public CategoriesController(ICategoryRepository repo, FunewsManagementContext db)
    {
        _repo = repo;
        _db = db;
    }

    public sealed class CategoryCreateRequest
    {
        [Required]
        public string CategoryName { get; set; } = default!;
        public string? CategoryDesciption { get; set; }
        public short? ParentCategoryId { get; set; }
        public bool? IsActive { get; set; }
    }

    public sealed class CategoryUpdateRequest
    {
        public string? CategoryName { get; set; }
        public string? CategoryDesciption { get; set; }
        public short? ParentCategoryId { get; set; }
        public bool? IsActive { get; set; }
    }

    [HttpGet]
    [AllowAnonymous]
    [EnableQuery(PageSize = 50)]
    public IActionResult GetAll()
    {
        var query = _db.Categories.AsNoTracking();
        return Ok(query);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<Category>> GetById(short id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Category>> Create([FromBody] CategoryCreateRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        short? normalizedParent = request.ParentCategoryId.HasValue && request.ParentCategoryId.Value <= 0
            ? null
            : request.ParentCategoryId;

        if (normalizedParent.HasValue)
        {
            var parentExists = await _db.Categories.AnyAsync(c => c.CategoryId == normalizedParent.Value);
            if (!parentExists) return BadRequest("Parent category not found.");
        }

        var entity = new Category
        {
            CategoryName = request.CategoryName,
            CategoryDesciption = request.CategoryDesciption ?? string.Empty,
            ParentCategoryId = normalizedParent,
            IsActive = request.IsActive ?? true
        };

        await _repo.AddAsync(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.CategoryId }, entity);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(short id, [FromBody] CategoryUpdateRequest request)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.CategoryName))
            existing.CategoryName = request.CategoryName;

        if (request.CategoryDesciption != null)
            existing.CategoryDesciption = request.CategoryDesciption;

        if (request.ParentCategoryId.HasValue)
        {
            short? normalizedParent = request.ParentCategoryId.Value <= 0 ? null : request.ParentCategoryId;

            if (normalizedParent.HasValue)
            {
                if (normalizedParent.Value == id) return BadRequest("A category cannot be its own parent.");
                var parentExists = await _db.Categories.AnyAsync(c => c.CategoryId == normalizedParent.Value);
                if (!parentExists) return BadRequest("Parent category not found.");
            }

            existing.ParentCategoryId = normalizedParent;
        }

        if (request.IsActive.HasValue)
            existing.IsActive = request.IsActive;

        await _repo.UpdateAsync(existing);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(short id)
    {
        var inUse = await _db.NewsArticles.AnyAsync(n => n.CategoryId == id);
        if (inUse) return Conflict(new { message = "Cannot delete category: it is used by one or more news articles." });

        await _repo.DeleteAsync(id);
        return NoContent();
    }
}