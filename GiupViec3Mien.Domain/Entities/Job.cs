using System;
using GiupViec3Mien.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

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

    public Guid? AssignedWorkerId { get; set; }
    public User? AssignedWorker { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
