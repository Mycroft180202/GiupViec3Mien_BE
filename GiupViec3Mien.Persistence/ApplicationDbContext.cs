using GiupViec3Mien.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GiupViec3Mien.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<WorkerProfile> WorkerProfiles { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<Review> Reviews { get; set; }

    public DbSet<JobApplication> JobApplications { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<SubscriptionPackage> SubscriptionPackages { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }

    // News Feed
    public DbSet<NewsPost> NewsPosts { get; set; }

    // Training Courses
    public DbSet<TrainingCourse> TrainingCourses { get; set; }
    public DbSet<CourseLesson> CourseLessons { get; set; }
    public DbSet<CourseEnrollment> CourseEnrollments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ... existing configurations ...
        modelBuilder.Entity<JobApplication>()
            .HasOne(ja => ja.Job)
            .WithMany(j => j.Applications)
            .HasForeignKey(ja => ja.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobApplication>()
            .HasOne(ja => ja.Applicant)
            .WithMany()
            .HasForeignKey(ja => ja.ApplicantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Required explicitly for PostgreSQL Npgsql to treat specific strings as JSONB
        modelBuilder.Entity<User>()
            .Property(u => u.AdditionalInfo)
            .HasColumnType("jsonb");

        modelBuilder.Entity<WorkerProfile>()
            .Property(w => w.Skills)
            .HasColumnType("jsonb");

        modelBuilder.Entity<SubscriptionPackage>()
            .Property(s => s.AdditionalBenefits)
            .HasColumnType("jsonb");

        // Relationships
        modelBuilder.Entity<WorkerProfile>()
            .HasOne(w => w.User)
            .WithOne(u => u.WorkerProfile)
            .HasForeignKey<WorkerProfile>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Job>()
            .HasOne(j => j.Employer)
            .WithMany()
            .HasForeignKey(j => j.EmployerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Job>()
            .HasOne(j => j.AssignedWorker)
            .WithMany()
            .HasForeignKey(j => j.AssignedWorkerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Job)
            .WithMany()
            .HasForeignKey(r => r.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Reviewer)
            .WithMany()
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Reviewee)
            .WithMany()
            .HasForeignKey(r => r.RevieweeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(cm => cm.Sender)
            .WithMany()
            .HasForeignKey(cm => cm.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(cm => cm.Receiver)
            .WithMany()
            .HasForeignKey(cm => cm.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Recipient)
            .WithMany()
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAt });
            
        // ActivityLog – actor is optional (system actions have no actor)
        modelBuilder.Entity<ActivityLog>()
            .HasOne(a => a.Actor)
            .WithMany()
            .HasForeignKey(a => a.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ActivityLog>()
            .Property(a => a.Metadata)
            .HasColumnType("jsonb");

        // ── NewsPost ──────────────────────────────────────────────────────
        modelBuilder.Entity<NewsPost>()
            .Property(n => n.Tags)
            .HasColumnType("jsonb");

        modelBuilder.Entity<NewsPost>()
            .HasOne(n => n.Author)
            .WithMany()
            .HasForeignKey(n => n.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── TrainingCourse ────────────────────────────────────────────────
        modelBuilder.Entity<TrainingCourse>()
            .HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CourseLesson>()
            .HasOne(l => l.Course)
            .WithMany(c => c.Lessons)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure lessons are always ordered
        modelBuilder.Entity<CourseLesson>()
            .HasIndex(l => new { l.CourseId, l.Order });

        modelBuilder.Entity<CourseEnrollment>()
            .HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CourseEnrollment>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One user can only enroll in the same course once
        modelBuilder.Entity<CourseEnrollment>()
            .HasIndex(e => new { e.CourseId, e.UserId })
            .IsUnique();

        // Setup PostGIS extension (for later geography implementations)
        modelBuilder.HasPostgresExtension("postgis");
    }
}
