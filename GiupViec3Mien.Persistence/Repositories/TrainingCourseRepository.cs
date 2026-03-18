using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Domain.Enums;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Persistence.Repositories;

public class TrainingCourseRepository : ITrainingCourseRepository
{
    private readonly ApplicationDbContext _context;

    public TrainingCourseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Courses ──────────────────────────────────────────────────────────

    public async Task AddCourseAsync(TrainingCourse course)
    {
        await _context.TrainingCourses.AddAsync(course);
    }

    public async Task<TrainingCourse?> GetCourseByIdAsync(Guid id)
    {
        return await _context.TrainingCourses
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<TrainingCourse?> GetCourseWithLessonsAsync(Guid id)
    {
        return await _context.TrainingCourses
            .Include(c => c.Author)
            .Include(c => c.Lessons.OrderBy(l => l.Order))
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<TrainingCourse>> GetPublishedCoursesAsync(
        int page = 1,
        int pageSize = 20,
        CourseCategory? category = null,
        CourseLevel? level = null,
        string? searchTerm = null)
    {
        var query = _context.TrainingCourses
            .Include(c => c.Author)
            .Where(c => c.IsPublished);

        if (category.HasValue)
            query = query.Where(c => c.Category == category.Value);

        if (level.HasValue)
            query = query.Where(c => c.Level == level.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(term) ||
                c.Description.ToLower().Contains(term));
        }

        return await query
            .OrderByDescending(c => c.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<TrainingCourse>> GetAllCoursesAsync(int page = 1, int pageSize = 20)
    {
        return await _context.TrainingCourses
            .Include(c => c.Author)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountPublishedCoursesAsync(CourseCategory? category = null)
    {
        var query = _context.TrainingCourses.Where(c => c.IsPublished);
        if (category.HasValue) query = query.Where(c => c.Category == category.Value);
        return await query.CountAsync();
    }

    public async Task<int> CountAllCoursesAsync()
    {
        return await _context.TrainingCourses.CountAsync();
    }

    public Task DeleteCourseAsync(TrainingCourse course)
    {
        _context.TrainingCourses.Remove(course);
        return Task.CompletedTask;
    }

    // ── Lessons ──────────────────────────────────────────────────────────

    public async Task AddLessonAsync(CourseLesson lesson)
    {
        await _context.CourseLessons.AddAsync(lesson);
    }

    public async Task<CourseLesson?> GetLessonByIdAsync(Guid id)
    {
        return await _context.CourseLessons
            .Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public Task DeleteLessonAsync(CourseLesson lesson)
    {
        _context.CourseLessons.Remove(lesson);
        return Task.CompletedTask;
    }

    // ── Enrollments ──────────────────────────────────────────────────────

    public async Task AddEnrollmentAsync(CourseEnrollment enrollment)
    {
        await _context.CourseEnrollments.AddAsync(enrollment);
    }

    public async Task<CourseEnrollment?> GetEnrollmentAsync(Guid courseId, Guid userId)
    {
        return await _context.CourseEnrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.CourseId == courseId && e.UserId == userId);
    }

    public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByUserAsync(Guid userId)
    {
        return await _context.CourseEnrollments
            .Include(e => e.Course)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();
    }

    public async Task<int> CountEnrollmentsAsync(Guid courseId)
    {
        return await _context.CourseEnrollments.CountAsync(e => e.CourseId == courseId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
