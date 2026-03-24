using System;

using Microsoft.AspNetCore.Http;

namespace GiupViec3Mien.Services.DTOs.Job;

public class ApplyJobRequest
{
    public string? Message { get; set; }
    public decimal BidPrice { get; set; }
    public DateTime? AvailableStartDate { get; set; }
    public IFormFile? Cv { get; set; }
}
