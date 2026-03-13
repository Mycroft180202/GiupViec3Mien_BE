using System.ComponentModel.DataAnnotations;

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
}
