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
            
        // Setup PostGIS extension (for later geography implementations)
        modelBuilder.HasPostgresExtension("postgis");
    }
}
