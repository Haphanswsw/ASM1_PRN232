using System.Security.Claims;
using System.Text.Json.Serialization;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementAPI.Controllers.OData;

[Route("odata/NewsArticles")]
[Authorize(Roles = "Staff,Admin")]
public class NewsArticlesODataController : ODataController
{
    private readonly FunewsManagementContext _db;
    public NewsArticlesODataController(FunewsManagementContext db) => _db = db;

    public sealed class NewsArticleCreateDto
    {
        [JsonPropertyName("newsTitle")]
        public string? NewsTitle { get; set; }
        [JsonPropertyName("headline")]
        public string Headline { get; set; } = default!;
        [JsonPropertyName("newsContent")]
        public string? NewsContent { get; set; }
        [JsonPropertyName("newsSource")]
        public string? NewsSource { get; set; }
        [JsonPropertyName("categoryId")]
        public short? CategoryId { get; set; }
        [JsonPropertyName("newsStatus")]
        public bool? NewsStatus { get; set; }
    }

    public sealed class NewsArticleUpdateDto
    {
        [JsonPropertyName("newsTitle")]
        public string? NewsTitle { get; set; }
        [JsonPropertyName("headline")]
        public string? Headline { get; set; }
        [JsonPropertyName("newsContent")]
        public string? NewsContent { get; set; }
        [JsonPropertyName("newsSource")]
        public string? NewsSource { get; set; }
        [JsonPropertyName("categoryId")]
        public short? CategoryId { get; set; }
        [JsonPropertyName("newsStatus")]
        public bool? NewsStatus { get; set; }
    }

    private static object ShapeNewsResponse(NewsArticle e) => new
    {
        newsTitle = e.NewsTitle,
        headline = e.Headline,
        newsContent = e.NewsContent,
        newsSource = e.NewsSource,
        categoryId = e.CategoryId,
        newsStatus = e.NewsStatus
    };

    [HttpGet]
    [EnableQuery(PageSize = 50)]
    public IActionResult Get() => Ok(_db.NewsArticles.AsNoTracking());

    [HttpGet("({key})")]
    [EnableQuery]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        var item = await _db.NewsArticles.AsNoTracking().FirstOrDefaultAsync(n => n.NewsArticleId == key);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] NewsArticleCreateDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var currentUserId = short.Parse(userIdStr);

        if (dto.CategoryId.HasValue)
        {
            var exists = await _db.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId.Value);
            if (!exists) return BadRequest("Category not found.");
        }

        var entity = new NewsArticle
        {
            NewsArticleId = Guid.NewGuid().ToString("N").Substring(0, 20),
            NewsTitle = dto.NewsTitle,
            Headline = dto.Headline,
            NewsContent = dto.NewsContent,
            NewsSource = dto.NewsSource,
            CategoryId = dto.CategoryId,
            NewsStatus = dto.NewsStatus ?? true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = null,
            CreatedById = currentUserId,
            UpdatedById = null
        };

        _db.NewsArticles.Add(entity);
        await _db.SaveChangesAsync();

        // 201 với body đã shape để tránh OData CreatedODataResult
        return StatusCode(StatusCodes.Status201Created, ShapeNewsResponse(entity));
    }

    [HttpPut("({key})")]
    public async Task<IActionResult> Put([FromRoute] string key, [FromBody] NewsArticleUpdateDto dto)
    {
        var existing = await _db.NewsArticles.FirstOrDefaultAsync(n => n.NewsArticleId == key);
        if (existing == null) return NotFound();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var currentUserId = short.Parse(userIdStr);

        if (existing.CreatedById != currentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, "Only the creator can update this article.");

        if (dto.CategoryId.HasValue)
        {
            var catExists = await _db.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId.Value);
            if (!catExists) return BadRequest("Category not found.");
        }

        if (dto.NewsTitle != null) existing.NewsTitle = dto.NewsTitle;
        if (!string.IsNullOrWhiteSpace(dto.Headline)) existing.Headline = dto.Headline;
        if (dto.NewsContent != null) existing.NewsContent = dto.NewsContent;
        if (dto.NewsSource != null) existing.NewsSource = dto.NewsSource;
        if (dto.CategoryId.HasValue) existing.CategoryId = dto.CategoryId;
        if (dto.NewsStatus.HasValue) existing.NewsStatus = dto.NewsStatus;

        existing.ModifiedDate = DateTime.UtcNow;
        existing.UpdatedById = currentUserId;

        await _db.SaveChangesAsync();
        return Ok(ShapeNewsResponse(existing));
    }

    [HttpPatch("({key})")]
    public async Task<IActionResult> Patch([FromRoute] string key, [FromBody] Delta<NewsArticle> patch)
    {
        var existing = await _db.NewsArticles.FirstOrDefaultAsync(n => n.NewsArticleId == key);
        if (existing == null) return NotFound();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var currentUserId = short.Parse(userIdStr);

        if (existing.CreatedById != currentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, "Only the creator can update this article.");

        var temp = new NewsArticle();
        patch.Patch(temp);

        var changed = patch.GetChangedPropertyNames().ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (changed.Contains(nameof(NewsArticle.NewsTitle)))
            existing.NewsTitle = temp.NewsTitle;
        if (changed.Contains(nameof(NewsArticle.Headline)) && !string.IsNullOrWhiteSpace(temp.Headline))
            existing.Headline = temp.Headline;
        if (changed.Contains(nameof(NewsArticle.NewsContent)))
            existing.NewsContent = temp.NewsContent;
        if (changed.Contains(nameof(NewsArticle.NewsSource)))
            existing.NewsSource = temp.NewsSource;
        if (changed.Contains(nameof(NewsArticle.CategoryId)))
        {
            if (temp.CategoryId.HasValue)
            {
                var catExists = await _db.Categories.AnyAsync(c => c.CategoryId == temp.CategoryId.Value);
                if (!catExists) return BadRequest("Category not found.");
            }
            existing.CategoryId = temp.CategoryId;
        }
        if (changed.Contains(nameof(NewsArticle.NewsStatus)) && temp.NewsStatus.HasValue)
            existing.NewsStatus = temp.NewsStatus;

        existing.ModifiedDate = DateTime.UtcNow;
        existing.UpdatedById = currentUserId;

        await _db.SaveChangesAsync();
        return Ok(ShapeNewsResponse(existing));
    }

    [HttpDelete("({key})")]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        var existing = await _db.NewsArticles.FirstOrDefaultAsync(n => n.NewsArticleId == key);
        if (existing == null) return NotFound();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var currentUserId = short.Parse(userIdStr);

        if (existing.CreatedById != currentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, "Only the creator can delete this article.");

        _db.NewsArticles.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}