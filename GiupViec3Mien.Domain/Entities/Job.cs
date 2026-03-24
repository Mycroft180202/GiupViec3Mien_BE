using System;
using GiupViec3Mien.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace GiupViec3Mien.Domain.Entities;

public class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid EmployerId { get; set; }
    public User? Employer { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty; // Address string
    
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public decimal Price { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Open;

    public PostType PostType { get; set; } = PostType.Hiring;
    public JobTimingType TimingType { get; set; } = JobTimingType.PartTime;
    public ServiceCategory ServiceCategory { get; set; } = ServiceCategory.Housekeeping;
    
    public string? WorkingTimeDescription { get; set; } // e.g., "Sáng 8h-11h"
    public DateTime? WorkDate { get; set; }
    public string? WorkStartTime { get; set; }
    public string? WorkEndTime { get; set; }
    public GenderOption PreferredGender { get; set; } = GenderOption.Any;
    public string? TargetAgeRange { get; set; } // e.g., "20-40"

    [Column(TypeName = "jsonb")]
    public string? RequiredSkills { get; set; } // JSON array of required skills

    public Guid? AssignedWorkerId { get; set; }
    public User? AssignedWorker { get; set; }

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
