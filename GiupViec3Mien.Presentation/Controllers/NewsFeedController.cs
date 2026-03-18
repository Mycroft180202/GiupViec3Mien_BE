using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GiupViec3Mien.Services.NewsFeed;
using GiupViec3Mien.Services.DTOs.NewsFeed;
using GiupViec3Mien.Domain.Enums;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GiupViec3Mien.Presentation.Controllers;

/// <summary>
/// Bảng tin hệ thống (News Feed).
///
/// PUBLIC  – GET /api/newsfeed           → danh sách bài đã xuất bản (phân trang, lọc danh mục)
/// PUBLIC  – GET /api/newsfeed/{id}      → chi tiết bài viết
/// ADMIN   – GET /api/newsfeed/admin     → tất cả bài (kể cả nháp)
/// ADMIN   – POST /api/newsfeed          → tạo bài mới
/// ADMIN   – PUT /api/newsfeed/{id}      → cập nhật bài
/// ADMIN   – DELETE /api/newsfeed/{id}   → xóa bài
/// ADMIN   – POST /api/newsfeed/{id}/publish    → xuất bản
/// ADMIN   – POST /api/newsfeed/{id}/unpublish  → hủy xuất bản
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NewsFeedController : ControllerBase
{
    private readonly INewsService _newsService;

    public NewsFeedController(INewsService newsService)
    {
        _newsService = newsService;
    }

    // ═══════════════════════════════════════════════════════
    // PUBLIC ENDPOINTS
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Danh sách bài viết đã xuất bản – có thể lọc theo danh mục và từ khóa.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublishedPosts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] NewsFeedCategory? category = null,
        [FromQuery] string? q = null)
    {
        if (pageSize > 100) pageSize = 100;
        var result = await _newsService.GetPublishedPostsAsync(page, pageSize, category, q);
        return Ok(result);
    }

    /// <summary>
    /// Chi tiết một bài viết. Tự động tăng lượt xem.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPostDetail(Guid id)
    {
        var post = await _newsService.GetPostDetailAsync(id);
        if (post == null) return NotFound(new { message = "Bài viết không tồn tại hoặc chưa được xuất bản." });
        return Ok(post);
    }

    // ═══════════════════════════════════════════════════════
    // ADMIN ENDPOINTS
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// [Admin] Tất cả bài viết (kể cả nháp), phân trang.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllPosts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageSize > 100) pageSize = 100;
        var result = await _newsService.GetAllPostsAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Tạo bài viết mới (có thể lưu nháp hoặc xuất bản ngay).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePost([FromBody] CreateNewsPostRequest request)
    {
        var authorId = GetCurrentUserId();
        if (authorId == Guid.Empty)
            return Unauthorized(new { message = "Không xác định được admin." });

        var post = await _newsService.CreatePostAsync(authorId, request);
        return CreatedAtAction(nameof(GetPostDetail), new { id = post.Id }, post);
    }

    /// <summary>
    /// [Admin] Cập nhật nội dung bài viết.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePost(Guid id, [FromBody] UpdateNewsPostRequest request)
    {
        var post = await _newsService.UpdatePostAsync(id, request);
        if (post == null) return NotFound(new { message = "Bài viết không tồn tại." });
        return Ok(post);
    }

    /// <summary>
    /// [Admin] Xóa bài viết.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var success = await _newsService.DeletePostAsync(id);
        if (!success) return NotFound(new { message = "Bài viết không tồn tại." });
        return Ok(new { Message = "Bài viết đã được xóa." });
    }

    /// <summary>
    /// [Admin] Xuất bản bài viết đang ở trạng thái nháp.
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PublishPost(Guid id)
    {
        var post = await _newsService.PublishPostAsync(id);
        if (post == null) return NotFound(new { message = "Bài viết không tồn tại." });
        return Ok(new { Message = "Bài viết đã được xuất bản.", Data = post });
    }

    /// <summary>
    /// [Admin] Hủy xuất bản (chuyển về nháp).
    /// </summary>
    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnpublishPost(Guid id)
    {
        var post = await _newsService.UnpublishPostAsync(id);
        if (post == null) return NotFound(new { message = "Bài viết không tồn tại." });
        return Ok(new { Message = "Bài viết đã bị hủy xuất bản.", Data = post });
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
