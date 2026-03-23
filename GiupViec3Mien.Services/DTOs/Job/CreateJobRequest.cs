using System.ComponentModel.DataAnnotations;
using GiupViec3Mien.Domain.Enums;

namespace GiupViec3Mien.Services.DTOs.Job;

public class CreateJobRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string Location { get; set; } = string.Empty;
    
    [Required]
    public decimal Price { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<string>? RequiredSkills { get; set; }

    public PostType PostType { get; set; } = PostType.Hiring;
    public JobTimingType TimingType { get; set; } = JobTimingType.PartTime;
    public ServiceCategory ServiceCategory { get; set; } = ServiceCategory.Housekeeping;
    
    public string? WorkingTimeDescription { get; set; }
    public GenderOption PreferredGender { get; set; } = GenderOption.Any;
    public string? TargetAgeRange { get; set; }
}
