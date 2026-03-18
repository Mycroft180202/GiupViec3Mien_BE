using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GiupViec3Mien.Services.Training;
using GiupViec3Mien.Services.DTOs.Training;
using GiupViec3Mien.Domain.Enums;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GiupViec3Mien.Presentation.Controllers;

/// <summary>
/// Khóa đào tạo kỹ năng dành cho người giúp việc.
///
/// PUBLIC  – GET /api/courses                          → danh sách khóa đã xuất bản
/// PUBLIC  – GET /api/courses/{id}                     → chi tiết khóa (bài học bị ẩn nội dung trừ preview)
/// AUTH    – POST /api/courses/{id}/enroll             → đăng ký khóa học
/// AUTH    – POST /api/courses/{id}/complete           → đánh dấu hoàn thành
/// AUTH    – GET  /api/courses/my-courses              → khóa học của tôi
/// ADMIN   – GET  /api/courses/admin                   → tất cả khóa (kể cả chưa xuất bản)
/// ADMIN   – POST /api/courses                         → tạo khóa mới
/// ADMIN   – PUT  /api/courses/{id}                    → cập nhật khóa
/// ADMIN   – DELETE /api/courses/{id}                  → xóa khóa
/// ADMIN   – POST /api/courses/{id}/publish            → xuất bản
/// ADMIN   – POST /api/courses/{id}/unpublish          → hủy xuất bản
/// ADMIN   – POST /api/courses/{id}/lessons            → thêm bài học
/// ADMIN   – PUT  /api/courses/lessons/{lessonId}      → sửa bài học
/// ADMIN   – DELETE /api/courses/lessons/{lessonId}    → xóa bài học
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TrainingCourseController : ControllerBase
{
    private readonly ITrainingCourseService _courseService;

    public TrainingCourseController(ITrainingCourseService courseService)
    {
        _courseService = courseService;
    }

    // ═══════════════════════════════════════════════════════
    // PUBLIC ENDPOINTS
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Danh sách khóa học đã xuất bản. Có thể lọc theo danh mục, cấp độ, từ khóa.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublishedCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] CourseCategory? category = null,
        [FromQuery] CourseLevel? level = null,
        [FromQuery] string? q = null)
    {
        if (pageSize > 100) pageSize = 100;
        var result = await _courseService.GetPublishedCoursesAsync(page, pageSize, category, level, q);
        return Ok(result);
    }

    /// <summary>
    /// Chi tiết khóa học. Nội dung bài học chỉ hiển thị nếu người dùng đã đăng ký
    /// hoặc bài học được đánh dấu là preview.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCourseDetail(Guid id)
    {
        var requesterId = GetCurrentUserId();
        var course = await _courseService.GetCourseDetailAsync(id, requesterId == Guid.Empty ? null : requesterId);
        if (course == null) return NotFound(new { message = "Khóa học không tồn tại hoặc chưa được xuất bản." });
        return Ok(course);
    }

    // ═══════════════════════════════════════════════════════
    // AUTHENTICATED USER ENDPOINTS
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Đăng ký tham gia khóa học.
    /// </summary>
    [HttpPost("{id:guid}/enroll")]
    [Authorize]
    public async Task<IActionResult> EnrollInCourse(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();
        try
        {
            var enrollment = await _courseService.EnrollAsync(id, userId);
            return Ok(new { Message = "Đăng ký khóa học thành công.", Data = enrollment });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Đánh dấu đã hoàn thành khóa học.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize]
    public async Task<IActionResult> MarkCompleted(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _courseService.MarkCourseCompletedAsync(id, userId);
        if (result == null) return BadRequest(new { message = "Bạn chưa đăng ký khóa học này." });
        return Ok(new { Message = "Chúc mừng! Bạn đã hoàn thành khóa học.", Data = result });
    }

    /// <summary>
    /// Danh sách khóa học tôi đã đăng ký.
    /// </summary>
    [HttpGet("my-courses")]
    [Authorize]
    public async Task<IActionResult> GetMyCourses()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var courses = await _courseService.GetMyCoursesAsync(userId);
        return Ok(courses);
    }

    // ═══════════════════════════════════════════════════════
    // ADMIN – COURSE MANAGEMENT
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// [Admin] Tất cả khóa học (kể cả chưa xuất bản), phân trang.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageSize > 100) pageSize = 100;
        var result = await _courseService.GetAllCoursesAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Tạo khóa đào tạo mới.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
    {
        var authorId = GetCurrentUserId();
        if (authorId == Guid.Empty) return Unauthorized();

        var course = await _courseService.CreateCourseAsync(authorId, request);
        return CreatedAtAction(nameof(GetCourseDetail), new { id = course.Id }, course);
    }

    /// <summary>
    /// [Admin] Cập nhật thông tin khóa học.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCourse(Guid id, [FromBody] UpdateCourseRequest request)
    {
        var course = await _courseService.UpdateCourseAsync(id, request);
        if (course == null) return NotFound(new { message = "Khóa học không tồn tại." });
        return Ok(course);
    }

    /// <summary>
    /// [Admin] Xóa khóa học (cascade: bài học + đăng ký).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCourse(Guid id)
    {
        var success = await _courseService.DeleteCourseAsync(id);
        if (!success) return NotFound(new { message = "Khóa học không tồn tại." });
        return Ok(new { Message = "Khóa học đã được xóa." });
    }

    /// <summary>
    /// [Admin] Xuất bản khóa học.
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PublishCourse(Guid id)
    {
        var course = await _courseService.PublishCourseAsync(id);
        if (course == null) return NotFound(new { message = "Khóa học không tồn tại." });
        return Ok(new { Message = "Khóa học đã được xuất bản.", Data = course });
    }

    /// <summary>
    /// [Admin] Hủy xuất bản khóa học.
    /// </summary>
    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnpublishCourse(Guid id)
    {
        var course = await _courseService.UnpublishCourseAsync(id);
        if (course == null) return NotFound(new { message = "Khóa học không tồn tại." });
        return Ok(new { Message = "Khóa học đã bị hủy xuất bản.", Data = course });
    }

    // ═══════════════════════════════════════════════════════
    // ADMIN – LESSON MANAGEMENT
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// [Admin] Thêm bài học vào khóa học.
    /// </summary>
    [HttpPost("{id:guid}/lessons")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddLesson(Guid id, [FromBody] CreateLessonRequest request)
    {
        try
        {
            var lesson = await _courseService.AddLessonAsync(id, request);
            return Ok(new { Message = "Bài học đã được thêm.", Data = lesson });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// [Admin] Cập nhật nội dung bài học.
    /// </summary>
    [HttpPut("lessons/{lessonId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateLesson(Guid lessonId, [FromBody] UpdateLessonRequest request)
    {
        var lesson = await _courseService.UpdateLessonAsync(lessonId, request);
        if (lesson == null) return NotFound(new { message = "Bài học không tồn tại." });
        return Ok(lesson);
    }

    /// <summary>
    /// [Admin] Xóa bài học.
    /// </summary>
    [HttpDelete("lessons/{lessonId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteLesson(Guid lessonId)
    {
        var success = await _courseService.DeleteLessonAsync(lessonId);
        if (!success) return NotFound(new { message = "Bài học không tồn tại." });
        return Ok(new { Message = "Bài học đã được xóa." });
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
