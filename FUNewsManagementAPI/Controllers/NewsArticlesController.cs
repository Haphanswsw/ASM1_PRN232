using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repositories.Repositories;
using Microsoft.AspNetCore.OData.Query; // added

namespace FUNewsManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsArticlesController : ControllerBase
{
    private readonly INewsArticleRepository _repo;
    private readonly FunewsManagementContext _db;

    public NewsArticlesController(INewsArticleRepository repo, FunewsManagementContext db)
    {
        _repo = repo;
        _db = db;
    }

    public sealed class CreateNewsArticleRequest
    {
        public string? NewsTitle { get; set; }
        [Required] public string Headline { get; set; } = default!;
        public string? NewsContent { get; set; }
        public string? NewsSource { get; set; }
        public short? CategoryId { get; set; }
        public bool? NewsStatus { get; set; }
    }

    public sealed class UpdateNewsArticleRequest
    {
        public string? NewsTitle { get; set; }
        public string? Headline { get; set; }
        public string? NewsContent { get; set; }
        public string? NewsSource { get; set; }
        public short? CategoryId { get; set; }
        public bool? NewsStatus { get; set; }
    }
    public sealed class UpdateTagsRequest
    {
        public List<int> TagIds { get; set; } = new();
    }

    // Public can view active news
    [HttpGet("public")]
    [AllowAnonymous]
    [EnableQuery(PageSize = 50)]
    public IActionResult GetPublicActive()
    {
        var query = _db.NewsArticles
            .AsNoTracking()
            .Where(n => n.NewsStatus == true)
            .OrderByDescending(n => n.CreatedDate);

        return Ok(query);
    }

    [HttpGet("public/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<NewsArticle>> GetPublicById(string id)
    {
        var item = await _db.NewsArticles
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.NewsArticleId == id && n.NewsStatus == true);
        if (item == null) return NotFound();
        return Ok(item);
    }

    // Staff/Admin management
    [HttpGet]
    [Authorize(Roles = "Staff,Admin")]
    [EnableQuery(PageSize = 50)]
    public IActionResult GetAll()
    {
        // Expose IQueryable so OData can apply $filter/$orderby/$expand server-side
        var query = _db.NewsArticles.AsNoTracking();
        return Ok(query);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<NewsArticle>> GetById(string id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<NewsArticle>> Create([FromBody] CreateNewsArticleRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (request.CategoryId.HasValue)
        {
            var catExists = await _db.Categories.AnyAsync(c => c.CategoryId == request.CategoryId.Value);
            if (!catExists) return BadRequest("Category not found.");
        }

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var currentUserId = short.Parse(userIdStr);

        var entity = new NewsArticle
        {
            NewsArticleId = Guid.NewGuid().ToString("N").Substring(0, 20),
            NewsTitle = request.NewsTitle,
            Headline = request.Headline,
            NewsContent = request.NewsContent,
            NewsSource = request.NewsSource,
            CategoryId = request.CategoryId,
            NewsStatus = request.NewsStatus ?? true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = null,
            CreatedById = currentUserId,
            UpdatedById = null
        };

        await _repo.AddAsync(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.NewsArticleId }, entity);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateNewsArticleRequest request)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        if (request.CategoryId.HasValue)
        {
            var catExists = await _db.Categories.AnyAsync(c => c.CategoryId == request.CategoryId.Value);
            if (!catExists) return BadRequest("Category not found.");
        }

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var currentUserId = short.Parse(userIdStr);

        if (existing.CreatedById != currentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, "Only the creator can update this article.");

        if (request.NewsTitle != null) existing.NewsTitle = request.NewsTitle;
        if (!string.IsNullOrWhiteSpace(request.Headline)) existing.Headline = request.Headline;
        if (request.NewsContent != null) existing.NewsContent = request.NewsContent;
        if (request.NewsSource != null) existing.NewsSource = request.NewsSource;
        if (request.CategoryId.HasValue) existing.CategoryId = request.CategoryId;
        if (request.NewsStatus.HasValue) existing.NewsStatus = request.NewsStatus;

        existing.ModifiedDate = DateTime.UtcNow;
        existing.UpdatedById = currentUserId;

        await _repo.UpdateAsync(existing);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var currentUserId = short.Parse(userIdStr);

        if (existing.CreatedById != currentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, "Only the creator can delete this article.");

        await _repo.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Staff,Admin")]
    [EnableQuery(PageSize = 50)]
    public IActionResult GetMine()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var currentUserId = short.Parse(userIdStr);

        var query = _db.NewsArticles
            .AsNoTracking()
            .Where(n => n.CreatedById == currentUserId)
            .OrderByDescending(n => n.CreatedDate);

        return Ok(query);
    }

    [HttpGet("report")]
    [Authorize(Roles = "Admin")]
    [EnableQuery(PageSize = 100)]
    public IActionResult GetReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var start = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

        if (end.TimeOfDay == TimeSpan.Zero)
            end = end.Date.AddDays(1).AddTicks(-1);

        if (end < start)
            return BadRequest(new { message = "endDate must be >= startDate." });

        var query = _db.NewsArticles
            .AsNoTracking()
            .Where(n => n.CreatedDate >= start && n.CreatedDate <= end);

        return Ok(query);
    }

    [HttpPut("{id}/tags")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> ReplaceTags(string id, [FromBody] UpdateTagsRequest request)
    {
        var article = await _db.NewsArticles.Include(n => n.Tags).FirstOrDefaultAsync(n => n.NewsArticleId == id);
        if (article == null) return NotFound();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var currentUserId = short.Parse(userIdStr);

        if (article.CreatedById != currentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, "Only the creator can modify this article's tags.");

        var tags = await _db.Tags.Where(t => request.TagIds.Contains(t.TagId)).ToListAsync();
        article.Tags.Clear();
        foreach (var t in tags)
        {
            article.Tags.Add(t);
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }
}