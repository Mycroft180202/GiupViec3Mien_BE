using System;
using GiupViec3Mien.Domain.Enums;

namespace GiupViec3Mien.Services.DTOs.Job;

public class JobSearchFilters
{
    public string? Keyword { get; set; }
    public ServiceCategory? Category { get; set; }
    public string? Location { get; set; } // General string match for now
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public JobTimingType? Timing { get; set; }
    public PostType PostType { get; set; } = PostType.Hiring; // Default to hiring
    
    // Geographical filter (optional)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? RadiusKm { get; set; }
}
