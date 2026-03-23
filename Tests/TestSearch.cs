using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using GiupViec3Mien.Services.Elastic;
using GiupViec3Mien.Services.DTOs.Job;
using GiupViec3Mien.Domain.Enums;

namespace TestNamespace;

public class TestSearch
{
    public static async Task Main()
    {
        var settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"))
            .DefaultIndex("jobs");
        var client = new ElasticsearchClient(settings);
        var searchService = new JobSearchService(client);

        await searchService.InitializeIndexAsync();
        
        var sampleJob = new JobDocument
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Dọn dẹp nhà Cầu Giấy (Test Search)",
            Description = "Làm việc theo giờ cho hộ gia đình",
            Location = "Cầu Giấy, Hà Nội",
            Category = "housekeeping",
            PostType = "hiring",
            Price = 100000,
            CreatedAt = DateTime.UtcNow,
            EmployerName = "Anh Tuấn"
        };
        await searchService.BulkIndexAsync(new[] { sampleJob });
        
        System.Threading.Thread.Sleep(2000);

        Console.WriteLine("--- SEARCH ALL (MatchAll) ---");
        var allResults = await client.SearchAsync<JobDocument>(s => s.Indices("jobs").Query(q => q.MatchAll(m => {})));
        Console.WriteLine($"Total indexed: {allResults.Documents.Count}");

        Console.WriteLine("\n--- SERVICE SEARCH (Hiring) ---");
        var filters = new JobSearchFilters { PostType = PostType.Hiring };
        var results = await searchService.SearchAsync(filters);

        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        Console.WriteLine(json);
    }
}
