using System.Text.Json.Serialization;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementAPI.Controllers.OData;

[Route("odata/Categories")]
public class CategoriesODataController : ODataController
{
    private readonly FunewsManagementContext _db;
    public CategoriesODataController(FunewsManagementContext db) => _db = db;

    public sealed class CategoryWriteDto
    {
        [JsonPropertyName("categoryName")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("categoryDesciption")]
        public string? CategoryDesciption { get; set; }

        [JsonPropertyName("parentCategoryId")]
        public short? ParentCategoryId { get; set; }

        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }
    }

    private static object ShapeCategoryResponse(Category e) => new
    {
        categoryName = e.CategoryName,
        categoryDesciption = e.CategoryDesciption,
        parentCategoryId = e.ParentCategoryId,
        isActive = e.IsActive
    };

    [HttpGet]
    [AllowAnonymous]
    [EnableQuery(PageSize = 50)]
    public IActionResult Get() => Ok(_db.Categories.AsNoTracking());

    [HttpGet("({key})")]
    [AllowAnonymous]
    [EnableQuery]
    public async Task<IActionResult> Get([FromRoute] short key)
    {
        var item = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == key);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> Post([FromBody] CategoryWriteDto dto)
    {
        short? normalizedParent = dto.ParentCategoryId.HasValue && dto.ParentCategoryId.Value <= 0
            ? null
            : dto.ParentCategoryId;

        if (normalizedParent.HasValue)
        {
            var exists = await _db.Categories.AnyAsync(c => c.CategoryId == normalizedParent.Value);
            if (!exists) return BadRequest("Parent category not found.");
        }

        var entity = new Category
        {
            CategoryName = dto.CategoryName,
            CategoryDesciption = dto.CategoryDesciption,
            ParentCategoryId = normalizedParent,
            IsActive = dto.IsActive ?? true
        };

        _db.Categories.Add(entity);
        await _db.SaveChangesAsync();

        // Trả về 201 với object đã shape để tránh OData CreatedODataResult
        return StatusCode(StatusCodes.Status201Created, ShapeCategoryResponse(entity));
    }

    [HttpPut("({key})")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> Put([FromRoute] short key, [FromBody] CategoryWriteDto dto)
    {
        var existing = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == key);
        if (existing == null) return NotFound();

        short? normalizedParent = dto.ParentCategoryId.HasValue && dto.ParentCategoryId.Value <= 0
            ? null
            : dto.ParentCategoryId;

        if (normalizedParent.HasValue)
        {
            if (normalizedParent.Value == key) return BadRequest("A category cannot be its own parent.");
            var exists = await _db.Categories.AnyAsync(c => c.CategoryId == normalizedParent.Value);
            if (!exists) return BadRequest("Parent category not found.");
        }

        existing.CategoryName = dto.CategoryName;
        existing.CategoryDesciption = dto.CategoryDesciption;
        existing.ParentCategoryId = normalizedParent;
        if (dto.IsActive.HasValue) existing.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();
        return Ok(ShapeCategoryResponse(existing));
    }

    [HttpPatch("({key})")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> Patch([FromRoute] short key, [FromBody] Delta<Category> patch)
    {
        var existing = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == key);
        if (existing == null) return NotFound();

        var temp = new Category();
        patch.Patch(temp);

        if (patch.GetChangedPropertyNames().Contains(nameof(Category.CategoryName)))
            existing.CategoryName = temp.CategoryName;

        if (patch.GetChangedPropertyNames().Contains(nameof(Category.CategoryDesciption)))
            existing.CategoryDesciption = temp.CategoryDesciption;

        if (patch.GetChangedPropertyNames().Contains(nameof(Category.ParentCategoryId)))
        {
            short? normalizedParent = temp.ParentCategoryId.HasValue && temp.ParentCategoryId.Value <= 0
                ? null
                : temp.ParentCategoryId;

            if (normalizedParent.HasValue)
            {
                if (normalizedParent.Value == key) return BadRequest("A category cannot be its own parent.");
                var exists = await _db.Categories.AnyAsync(c => c.CategoryId == normalizedParent.Value);
                if (!exists) return BadRequest("Parent category not found.");
            }
            existing.ParentCategoryId = normalizedParent;
        }

        if (patch.GetChangedPropertyNames().Contains(nameof(Category.IsActive)) && temp.IsActive.HasValue)
            existing.IsActive = temp.IsActive;

        await _db.SaveChangesAsync();
        return Ok(ShapeCategoryResponse(existing));
    }

    [HttpDelete("({key})")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> Delete([FromRoute] short key)
    {
        var inUse = await _db.NewsArticles.AnyAsync(n => n.CategoryId == key);
        if (inUse) return Conflict(new { message = "Cannot delete category: it is used by one or more news articles." });

        var existing = await _db.Categories.FindAsync(key);
        if (existing == null) return NotFound();

        _db.Categories.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}