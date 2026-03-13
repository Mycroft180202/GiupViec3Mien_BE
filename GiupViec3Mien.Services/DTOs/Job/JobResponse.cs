using System;
using GiupViec3Mien.Domain.Enums;

namespace GiupViec3Mien.Services.DTOs.Job;

public class JobResponse
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public JobStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
