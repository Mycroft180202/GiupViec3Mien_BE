using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface ITrainingCourseRepository
{
    // ── Courses ──────────────────────────────────────────────────────────
    Task AddCourseAsync(TrainingCourse course);
    Task<TrainingCourse?> GetCourseByIdAsync(Guid id);
    Task<TrainingCourse?> GetCourseWithLessonsAsync(Guid id);

    /// <summary>Khóa học đã xuất bản (public), có phân trang + lọc.</summary>
    Task<IEnumerable<TrainingCourse>> GetPublishedCoursesAsync(
        int page = 1,
        int pageSize = 20,
        CourseCategory? category = null,
        CourseLevel? level = null,
        string? searchTerm = null);

    /// <summary>Tất cả khóa học – dành cho admin.</summary>
    Task<IEnumerable<TrainingCourse>> GetAllCoursesAsync(int page = 1, int pageSize = 20);

    Task<int> CountPublishedCoursesAsync(CourseCategory? category = null);
    Task<int> CountAllCoursesAsync();
    Task DeleteCourseAsync(TrainingCourse course);

    // ── Lessons ──────────────────────────────────────────────────────────
    Task AddLessonAsync(CourseLesson lesson);
    Task<CourseLesson?> GetLessonByIdAsync(Guid id);
    Task DeleteLessonAsync(CourseLesson lesson);

    // ── Enrollments ──────────────────────────────────────────────────────
    Task AddEnrollmentAsync(CourseEnrollment enrollment);
    Task<CourseEnrollment?> GetEnrollmentAsync(Guid courseId, Guid userId);
    Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByUserAsync(Guid userId);
    Task<int> CountEnrollmentsAsync(Guid courseId);

    Task SaveChangesAsync();
}
