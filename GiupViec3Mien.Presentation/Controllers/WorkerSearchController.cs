using GiupViec3Mien.Services.DTOs.User;
using GiupViec3Mien.Services.Elastic;
using GiupViec3Mien.Services.UserServices;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkerSearchController : ControllerBase
{
    private readonly IWorkerSearchService _workerSearchService;
    private readonly IUserService _userService;

    public WorkerSearchController(IWorkerSearchService workerSearchService, IUserService userService)
    {
        _workerSearchService = workerSearchService;
        _userService = userService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] WorkerSearchFilters filters)
    {
        // Important: Map numeric ID strings (0, 1, 2, 3) to names used in index if needed
        if (!string.IsNullOrEmpty(filters.Category))
        {
            var mapping = new System.Collections.Generic.Dictionary<string, string>
            {
                { "0", "Giúp việc nhà" },
                { "1", "Trông trẻ" },
                { "2", "Chăm sóc người già" },
                { "3", "Nấu ăn" }
            };
            if (mapping.ContainsKey(filters.Category))
            {
                filters.Category = mapping[filters.Category];
            }
        }

        var workers = await _workerSearchService.SearchAsync(filters);
        return Ok(workers);
    }


    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex()
    {
        try
        {
            await _workerSearchService.InitializeIndexAsync();
            var workers = await _userService.GetAllWorkersWithProfilesAsync();
            
            var documents = new List<WorkerDocument>();
            foreach (var w in workers)
            {
                try
                {
                    documents.Add(new WorkerDocument
                    {
                        Id = w.Id.ToString(),
                        FullName = w.FullName,
                        AvatarUrl = w.AvatarUrl,
                        Bio = w.WorkerProfile?.Bio,
                        ExperienceYears = w.WorkerProfile?.ExperienceYears ?? 0,
                        HourlyRate = (double)(w.WorkerProfile?.HourlyRate ?? 0),
                        Skills = string.IsNullOrEmpty(w.WorkerProfile?.Skills) 
                            ? new List<string>() 
                            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(w.WorkerProfile.Skills) ?? new List<string>(),
                        Location = w.AdditionalInfo ?? string.Empty,
                        Verified = w.WorkerProfile?.Verified ?? false,
                        Coordinates = new JobGeoPoint(w.Latitude, w.Longitude),
                        CreatedAt = w.CreatedAt,
                        
                        // Populate categories from skills if available
                        ServiceCategories = string.IsNullOrEmpty(w.WorkerProfile?.Skills) 
                            ? new List<string>() 
                            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(w.WorkerProfile.Skills) ?? new List<string>(),
                        ServiceCategory = string.IsNullOrEmpty(w.WorkerProfile?.Skills) 
                            ? "Khác"
                            : (System.Text.Json.JsonSerializer.Deserialize<List<string>>(w.WorkerProfile.Skills)?.FirstOrDefault() ?? "Khác"),
                        TimingType = "parttime"
                    });


                }
                catch (Exception ex)
                {
                    // Log and continue
                    Console.WriteLine($"Error indexing worker {w.Id}: {ex.Message}");
                }
            }

            if (documents.Any())
            {
                await _workerSearchService.BulkIndexAsync(documents);
            }

            return Ok(new { message = "Worker indexing complete.", count = documents.Count, totalAttempted = workers.Count() });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, details = ex.ToString() });
        }
    }

}
