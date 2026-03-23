using System;
using System.Collections.Generic;

namespace GiupViec3Mien.Services.DTOs.User;

public class WorkerSearchFilters
{
    public string? Keyword { get; set; }
    public string? Location { get; set; }
    public decimal? MinRate { get; set; }
    public decimal? MaxRate { get; set; }
    public int? MinExpYears { get; set; }
    public List<string>? Skills { get; set; }
    public string? Category { get; set; }
    public string? Timing { get; set; }
    public bool? VerifiedOnly { get; set; }


    
    // Paging
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
