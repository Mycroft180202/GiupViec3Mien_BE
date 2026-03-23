using System;
using System.Collections.Generic;

namespace GiupViec3Mien.Services.Elastic;

public class WorkerDocument
{
    public string Id { get; set; } = string.Empty; // UserId
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public int ExperienceYears { get; set; }
    public double HourlyRate { get; set; }
    public List<string> Skills { get; set; } = new();
    public string Location { get; set; } = string.Empty; // From User or AdditionalInfo if available
    public bool Verified { get; set; }
    
    // Geo fields
    public JobGeoPoint? Coordinates { get; set; }

    
    // Categories (derived from bio or skills if needed, or if explicit)
    public string? ServiceCategory { get; set; }
    public List<string> ServiceCategories { get; set; } = new();

    public string? TimingType { get; set; }
    
    public DateTime CreatedAt { get; set; }

}
