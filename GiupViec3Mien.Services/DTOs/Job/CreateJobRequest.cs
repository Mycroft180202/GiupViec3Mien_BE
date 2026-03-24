using System.ComponentModel.DataAnnotations;
using GiupViec3Mien.Domain.Enums;

namespace GiupViec3Mien.Services.DTOs.Job;

public class CreateJobRequest
{
    [Required]
    [MinLength(5)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MinLength(10)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [MinLength(5)]
    public string Location { get; set; } = string.Empty;
    
    [Required]
    [Range(typeof(decimal), "1", "999999999")]
    public decimal Price { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<string>? RequiredSkills { get; set; }

    public PostType PostType { get; set; } = PostType.Hiring;
    public JobTimingType TimingType { get; set; } = JobTimingType.PartTime;
    public ServiceCategory ServiceCategory { get; set; } = ServiceCategory.Housekeeping;
    
    public string? WorkingTimeDescription { get; set; }
    public DateTime? WorkDate { get; set; }
    public string? WorkStartTime { get; set; }
    public string? WorkEndTime { get; set; }
    public GenderOption PreferredGender { get; set; } = GenderOption.Any;
    public string? TargetAgeRange { get; set; }
}
