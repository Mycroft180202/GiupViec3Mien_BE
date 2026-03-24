using System;
using System.Collections.Generic;
using GiupViec3Mien.Domain.Enums;

namespace GiupViec3Mien.Services.DTOs.Job;

public class JobResponse
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public string EmployerName { get; set; } = string.Empty;
    public string? EmployerAvatarUrl { get; set; }
    public string? EmployerPhone { get; set; }
    public string? EmployerEmail { get; set; }
    public string CompanyHotline { get; set; } = "1900-xxxx (Giúp Việc 3 Miền Support)";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<string>? RequiredSkills { get; set; }
    public JobStatus Status { get; set; }
    public PostType PostType { get; set; }
    public JobTimingType TimingType { get; set; }
    public ServiceCategory ServiceCategory { get; set; }
    
    public string? WorkingTimeDescription { get; set; }
    public DateTime? WorkDate { get; set; }
    public string? WorkStartTime { get; set; }
    public string? WorkEndTime { get; set; }
    public GenderOption PreferredGender { get; set; }
    public string? TargetAgeRange { get; set; }

    public int ApplicantCount { get; set; }
    public Guid? AssignedWorkerId { get; set; }
    public string? AssignedWorkerName { get; set; }
    public DateTime CreatedAt { get; set; }
}
