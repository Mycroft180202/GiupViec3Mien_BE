using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Domain.Enums;
using GiupViec3Mien.Services.DTOs.Training;
using GiupViec3Mien.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Training;

public class TrainingCourseService : ITrainingCourseService
{
    private readonly ITrainingCourseRepository _repo;
    private readonly IUserRepository _userRepo;

    public TrainingCourseService(ITrainingCourseRepository repo, IUserRepository userRepo)
    {
        _repo = repo;
        _userRepo = userRepo;
    }

    // ── Public / User ─────────────────────────────────────────────────────

    public async Task<CoursePagedResponse> GetPublishedCoursesAsync(
        int page = 1,
        int pageSize = 20,
        CourseCategory? category = null,
        CourseLevel? level = null,
        string? searchTerm = null)
    {
        var courses = await _repo.GetPublishedCoursesAsync(page, pageSize, category, level, searchTerm);
        var total = await _repo.CountPublishedCoursesAsync(category);

        return new CoursePagedResponse
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
            Data = courses.Select(c => MapToSummary(c, c.Lessons.Count, c.EnrollmentCount))
        };
    }

    public async Task<CourseDetailResponse?> GetCourseDetailAsync(Guid courseId, Guid? requesterId)
    {
        var course = await _repo.GetCourseWithLessonsAsync(courseId);
        if (course == null || !course.IsPublished) return null;

        CourseEnrollment? enrollment = null;
        bool hasFullAccess = false;

        if (requesterId.HasValue && requesterId.Value != Guid.Empty)
        {
            enrollment = await _repo.GetEnrollmentAsync(courseId, requesterId.Value);
            var requester = await _userRepo.GetByIdAsync(requesterId.Value);
            hasFullAccess = enrollment != null ||
                            requester?.Role == Role.Admin ||
                            (!course.RequiresPremium && (course.Price == null || course.Price == 0));
        }
        else
        {
            // Anonymous users can only see free non-premium courses' lesson titles
            hasFullAccess = !course.RequiresPremium && (course.Price == null || course.Price == 0);
        }

        var lessons = course.Lessons.OrderBy(l => l.Order).Select(l =>
            MapToLesson(l, hasFullAccess || l.IsPreview)).ToList();

        var detail = MapToDetail(course, lessons, course.EnrollmentCount);

        if (enrollment != null)
        {
            detail.MyEnrollment = new EnrollmentStatusResponse
            {
                EnrollmentId = enrollment.Id,
                CourseId = enrollment.CourseId,
                UserId = enrollment.UserId,
                IsCompleted = enrollment.IsCompleted,
                CompletedAt = enrollment.CompletedAt,
                EnrolledAt = enrollment.EnrolledAt
            };
        }

        return detail;
    }

    // ── Enrollment ────────────────────────────────────────────────────────

    public async Task<EnrollmentStatusResponse> EnrollAsync(Guid courseId, Guid userId)
    {
        var course = await _repo.GetCourseByIdAsync(courseId);
        if (course == null || !course.IsPublished)
            throw new Exception("Khóa học không tồn tại hoặc chưa mở.");

        var existing = await _repo.GetEnrollmentAsync(courseId, userId);
        if (existing != null)
            throw new Exception("Bạn đã đăng ký khóa học này rồi.");

        var enrollment = new CourseEnrollment
        {
            CourseId = courseId,
            UserId = userId
        };

        await _repo.AddEnrollmentAsync(enrollment);

        // Increment enrollment counter on the course
        course.EnrollmentCount++;
        course.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();

        return new EnrollmentStatusResponse
        {
            EnrollmentId = enrollment.Id,
            CourseId = enrollment.CourseId,
            UserId = enrollment.UserId,
            IsCompleted = false,
            EnrolledAt = enrollment.EnrolledAt
        };
    }

    public async Task<EnrollmentStatusResponse?> MarkCourseCompletedAsync(Guid courseId, Guid userId)
    {
        var enrollment = await _repo.GetEnrollmentAsync(courseId, userId);
        if (enrollment == null) return null;

        enrollment.IsCompleted = true;
        enrollment.CompletedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();

        return new EnrollmentStatusResponse
        {
            EnrollmentId = enrollment.Id,
            CourseId = enrollment.CourseId,
            UserId = enrollment.UserId,
            IsCompleted = true,
            CompletedAt = enrollment.CompletedAt,
            EnrolledAt = enrollment.EnrolledAt
        };
    }

    public async Task<IEnumerable<MyCourseResponse>> GetMyCoursesAsync(Guid userId)
    {
        var enrollments = await _repo.GetEnrollmentsByUserAsync(userId);
        return enrollments
            .Where(e => e.Course != null)
            .Select(e =>
            {
                var summary = MapToSummary(e.Course!, e.Course!.Lessons.Count, e.Course.EnrollmentCount);
                return new MyCourseResponse
                {
                    Id = summary.Id,
                    Title = summary.Title,
                    Description = summary.Description,
                    Category = summary.Category,
                    CategoryLabel = summary.CategoryLabel,
                    Level = summary.Level,
                    LevelLabel = summary.LevelLabel,
                    ThumbnailUrl = summary.ThumbnailUrl,
                    DurationMinutes = summary.DurationMinutes,
                    Price = summary.Price,
                    RequiresPremium = summary.RequiresPremium,
                    IsPublished = summary.IsPublished,
                    LessonCount = summary.LessonCount,
                    EnrollmentCount = summary.EnrollmentCount,
                    AuthorId = summary.AuthorId,
                    AuthorName = summary.AuthorName,
                    PublishedAt = summary.PublishedAt,
                    CreatedAt = summary.CreatedAt,
                    UpdatedAt = summary.UpdatedAt,
                    IsCompleted = e.IsCompleted,
                    CompletedAt = e.CompletedAt,
                    EnrolledAt = e.EnrolledAt
                };
            });
    }

    // ── Admin – Courses ───────────────────────────────────────────────────

    public async Task<CoursePagedResponse> GetAllCoursesAsync(int page = 1, int pageSize = 20)
    {
        var courses = await _repo.GetAllCoursesAsync(page, pageSize);
        var total = await _repo.CountAllCoursesAsync();

        return new CoursePagedResponse
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
            Data = courses.Select(c => MapToSummary(c, c.Lessons.Count, c.EnrollmentCount))
        };
    }

    public async Task<CourseDetailResponse> CreateCourseAsync(Guid authorId, CreateCourseRequest request)
    {
        var course = new TrainingCourse
        {
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Level = request.Level,
            ThumbnailUrl = request.ThumbnailUrl,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            RequiresPremium = request.RequiresPremium,
            IsPublished = request.IsPublished,
            AuthorId = authorId,
            PublishedAt = request.IsPublished ? DateTime.UtcNow : null
        };

        await _repo.AddCourseAsync(course);
        await _repo.SaveChangesAsync();

        var saved = await _repo.GetCourseWithLessonsAsync(course.Id);
        return MapToDetail(saved!, new List<LessonResponse>(), 0);
    }

    public async Task<CourseDetailResponse?> UpdateCourseAsync(Guid courseId, UpdateCourseRequest request)
    {
        var course = await _repo.GetCourseByIdAsync(courseId);
        if (course == null) return null;

        if (request.Title != null) course.Title = request.Title;
        if (request.Description != null) course.Description = request.Description;
        if (request.Category.HasValue) course.Category = request.Category.Value;
        if (request.Level.HasValue) course.Level = request.Level.Value;
        if (request.ThumbnailUrl != null) course.ThumbnailUrl = request.ThumbnailUrl;
        if (request.DurationMinutes.HasValue) course.DurationMinutes = request.DurationMinutes.Value;
        if (request.Price.HasValue) course.Price = request.Price.Value;
        if (request.RequiresPremium.HasValue) course.RequiresPremium = request.RequiresPremium.Value;

        if (request.IsPublished.HasValue)
        {
            var wasPublished = course.IsPublished;
            course.IsPublished = request.IsPublished.Value;
            if (!wasPublished && course.IsPublished)
                course.PublishedAt = DateTime.UtcNow;
            else if (!course.IsPublished)
                course.PublishedAt = null;
        }

        course.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync();

        var updated = await _repo.GetCourseWithLessonsAsync(courseId);
        var lessonCount = updated?.Lessons.Count ?? 0;
        var lessons = updated?.Lessons.OrderBy(l => l.Order)
            .Select(l => MapToLesson(l, true)).ToList() ?? new List<LessonResponse>();

        return MapToDetail(updated!, lessons, updated!.EnrollmentCount);
    }

    public async Task<bool> DeleteCourseAsync(Guid courseId)
    {
        var course = await _repo.GetCourseByIdAsync(courseId);
        if (course == null) return false;

        await _repo.DeleteCourseAsync(course);
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<CourseDetailResponse?> PublishCourseAsync(Guid courseId)
    {
        var course = await _repo.GetCourseByIdAsync(courseId);
        if (course == null) return null;

        course.IsPublished = true;
        course.PublishedAt ??= DateTime.UtcNow;
        course.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();
        return await GetAdminCourseDetailAsync(courseId);
    }

    public async Task<CourseDetailResponse?> UnpublishCourseAsync(Guid courseId)
    {
        var course = await _repo.GetCourseByIdAsync(courseId);
        if (course == null) return null;

        course.IsPublished = false;
        course.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();
        return await GetAdminCourseDetailAsync(courseId);
    }

    // ── Admin – Lessons ───────────────────────────────────────────────────

    public async Task<LessonResponse> AddLessonAsync(Guid courseId, CreateLessonRequest request)
    {
        var course = await _repo.GetCourseWithLessonsAsync(courseId);
        if (course == null) throw new Exception("Khóa học không tồn tại.");

        int order = request.Order ?? (course.Lessons.Any()
            ? course.Lessons.Max(l => l.Order) + 1
            : 0);

        var lesson = new CourseLesson
        {
            CourseId = courseId,
            Title = request.Title,
            Content = request.Content,
            Order = order,
            DurationMinutes = request.DurationMinutes,
            VideoUrl = request.VideoUrl,
            AttachmentUrl = request.AttachmentUrl,
            IsPreview = request.IsPreview
        };

        await _repo.AddLessonAsync(lesson);

        // Update total duration on course
        course.DurationMinutes += request.DurationMinutes;
        course.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();
        return MapToLesson(lesson, true);
    }

    public async Task<LessonResponse?> UpdateLessonAsync(Guid lessonId, UpdateLessonRequest request)
    {
        var lesson = await _repo.GetLessonByIdAsync(lessonId);
        if (lesson == null) return null;

        if (request.Title != null) lesson.Title = request.Title;
        if (request.Content != null) lesson.Content = request.Content;
        if (request.Order.HasValue) lesson.Order = request.Order.Value;
        if (request.VideoUrl != null) lesson.VideoUrl = request.VideoUrl;
        if (request.AttachmentUrl != null) lesson.AttachmentUrl = request.AttachmentUrl;
        if (request.IsPreview.HasValue) lesson.IsPreview = request.IsPreview.Value;

        if (request.DurationMinutes.HasValue)
        {
            var delta = request.DurationMinutes.Value - lesson.DurationMinutes;
            lesson.DurationMinutes = request.DurationMinutes.Value;

            // Update course total duration
            var course = await _repo.GetCourseByIdAsync(lesson.CourseId);
            if (course != null)
            {
                course.DurationMinutes = Math.Max(0, course.DurationMinutes + delta);
                course.UpdatedAt = DateTime.UtcNow;
            }
        }

        lesson.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync();
        return MapToLesson(lesson, true);
    }

    public async Task<bool> DeleteLessonAsync(Guid lessonId)
    {
        var lesson = await _repo.GetLessonByIdAsync(lessonId);
        if (lesson == null) return false;

        // Adjust course total duration
        var course = await _repo.GetCourseByIdAsync(lesson.CourseId);
        if (course != null)
        {
            course.DurationMinutes = Math.Max(0, course.DurationMinutes - lesson.DurationMinutes);
            course.UpdatedAt = DateTime.UtcNow;
        }

        await _repo.DeleteLessonAsync(lesson);
        await _repo.SaveChangesAsync();
        return true;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task<CourseDetailResponse?> GetAdminCourseDetailAsync(Guid courseId)
    {
        var course = await _repo.GetCourseWithLessonsAsync(courseId);
        if (course == null) return null;
        var lessons = course.Lessons.OrderBy(l => l.Order).Select(l => MapToLesson(l, true)).ToList();
        return MapToDetail(course, lessons, course.EnrollmentCount);
    }

    private static CourseSummaryResponse MapToSummary(TrainingCourse c, int lessonCount, int enrollmentCount) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Description = c.Description,
        Category = c.Category,
        CategoryLabel = GetCategoryLabel(c.Category),
        Level = c.Level,
        LevelLabel = GetLevelLabel(c.Level),
        ThumbnailUrl = c.ThumbnailUrl,
        DurationMinutes = c.DurationMinutes,
        Price = c.Price,
        RequiresPremium = c.RequiresPremium,
        IsPublished = c.IsPublished,
        LessonCount = lessonCount,
        EnrollmentCount = enrollmentCount,
        AuthorId = c.AuthorId,
        AuthorName = c.Author?.FullName ?? "Admin",
        PublishedAt = c.PublishedAt,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };

    private static CourseDetailResponse MapToDetail(
        TrainingCourse c,
        IEnumerable<LessonResponse> lessons,
        int enrollmentCount) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Description = c.Description,
        Category = c.Category,
        CategoryLabel = GetCategoryLabel(c.Category),
        Level = c.Level,
        LevelLabel = GetLevelLabel(c.Level),
        ThumbnailUrl = c.ThumbnailUrl,
        DurationMinutes = c.DurationMinutes,
        Price = c.Price,
        RequiresPremium = c.RequiresPremium,
        IsPublished = c.IsPublished,
        LessonCount = lessons.Count(),
        EnrollmentCount = enrollmentCount,
        AuthorId = c.AuthorId,
        AuthorName = c.Author?.FullName ?? "Admin",
        PublishedAt = c.PublishedAt,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        Lessons = lessons
    };

    private static LessonResponse MapToLesson(CourseLesson l, bool canView) => new()
    {
        Id = l.Id,
        CourseId = l.CourseId,
        Title = l.Title,
        Content = canView ? l.Content : null,
        Order = l.Order,
        DurationMinutes = l.DurationMinutes,
        VideoUrl = canView ? l.VideoUrl : null,
        AttachmentUrl = canView ? l.AttachmentUrl : null,
        IsPreview = l.IsPreview,
        CanView = canView,
        CreatedAt = l.CreatedAt,
        UpdatedAt = l.UpdatedAt
    };

    private static string GetCategoryLabel(CourseCategory c) => c switch
    {
        CourseCategory.Babysitting => "Kỹ năng chăm sóc trẻ",
        CourseCategory.ElderCare  => "Kỹ năng chăm sóc người cao tuổi",
        CourseCategory.Housekeeping => "Kỹ năng dọn dẹp nhà cửa",
        CourseCategory.Cooking    => "Kỹ năng nấu ăn",
        CourseCategory.SoftSkills => "Kỹ năng giao tiếp & ứng xử",
        _                         => "Khác"
    };

    private static string GetLevelLabel(CourseLevel l) => l switch
    {
        CourseLevel.Beginner     => "Cơ bản",
        CourseLevel.Intermediate => "Trung cấp",
        CourseLevel.Advanced     => "Nâng cao",
        _                        => l.ToString()
    };
}
