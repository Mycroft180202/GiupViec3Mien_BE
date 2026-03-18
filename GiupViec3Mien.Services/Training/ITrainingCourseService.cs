using GiupViec3Mien.Domain.Enums;
using GiupViec3Mien.Services.DTOs.Training;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Training;

public interface ITrainingCourseService
{
    // ── Public / User ─────────────────────────────────────────────────────
    Task<CoursePagedResponse> GetPublishedCoursesAsync(
        int page = 1,
        int pageSize = 20,
        CourseCategory? category = null,
        CourseLevel? level = null,
        string? searchTerm = null);

    /// <summary>Chi tiết khóa học. Nội dung bài học bị ẩn trừ preview/đã đăng ký.</summary>
    Task<CourseDetailResponse?> GetCourseDetailAsync(Guid courseId, Guid? requesterId);

    // ── Enrollment ────────────────────────────────────────────────────────
    Task<EnrollmentStatusResponse> EnrollAsync(Guid courseId, Guid userId);
    Task<EnrollmentStatusResponse?> MarkCourseCompletedAsync(Guid courseId, Guid userId);
    Task<IEnumerable<MyCourseResponse>> GetMyCoursesAsync(Guid userId);

    // ── Admin ─────────────────────────────────────────────────────────────
    Task<CoursePagedResponse> GetAllCoursesAsync(int page = 1, int pageSize = 20);
    Task<CourseDetailResponse> CreateCourseAsync(Guid authorId, CreateCourseRequest request);
    Task<CourseDetailResponse?> UpdateCourseAsync(Guid courseId, UpdateCourseRequest request);
    Task<bool> DeleteCourseAsync(Guid courseId);
    Task<CourseDetailResponse?> PublishCourseAsync(Guid courseId);
    Task<CourseDetailResponse?> UnpublishCourseAsync(Guid courseId);

    // ── Lesson management (Admin) ─────────────────────────────────────────
    Task<LessonResponse> AddLessonAsync(Guid courseId, CreateLessonRequest request);
    Task<LessonResponse?> UpdateLessonAsync(Guid lessonId, UpdateLessonRequest request);
    Task<bool> DeleteLessonAsync(Guid lessonId);
}
