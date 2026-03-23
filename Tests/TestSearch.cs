using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using GiupViec3Mien.Services.Elastic;
using GiupViec3Mien.Services.DTOs.Job;
using GiupViec3Mien.Domain.Enums;

namespace TestNamespace;

public class TestSearch
{
    public static async Task Main()
    {
        // Simple manual config for local testing
        var settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"))
            .DefaultIndex("jobs");
        var client = new ElasticsearchClient(settings);
        
        var searchService = new JobSearchService(client);
        
        // 0. Initialize Index
        Console.WriteLine("Initializing index...");
        await searchService.InitializeIndexAsync();

        // 0. Index a sample job for testing
        Console.WriteLine("Index a sample job for testing...");

        var sampleJob = new JobDocument
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Cần người nấu ăn gia đình",
            Description = "Nấu ăn buổi tối cho gia đình 4 người tại Quận 1",
            Location = "Quận 1, Hồ Chí Minh",
            Category = "Cooking",
            Price = 100000,
            Coordinates = new JobGeoPoint(10.7769, 106.7009),
            PostType = "Hiring",
            Status = "Open",
            CreatedAt = DateTime.UtcNow,
            EmployerName = "Anh Tuấn"
        };
        await searchService.BulkIndexAsync(new[] { sampleJob });

        Console.WriteLine($"PostType.Hiring.ToString() = '{PostType.Hiring.ToString()}'");
        Console.WriteLine($"PostType.Seeking.ToString() = '{PostType.Seeking.ToString()}'");

        System.Threading.Thread.Sleep(2000); // Wait for ES to refresh index
        
        // 0.5 Check Total Count (No filters)
        // var totalResponse = await client.CountAsync<JobDocument>(c => c.Indices("jobs"));
        // Console.WriteLine($"Total documents in ES: {totalResponse.Count}");


        // 1. Test standard "Hiring" search (Initial load)


        Console.WriteLine("--- SEARCH RESULTS (HIRING) ---");
        var filtersHiring = new JobSearchFilters { PostType = PostType.Hiring };
        var resultsHiring = await searchService.SearchAsync(filtersHiring);
        PrintResults(resultsHiring);

        // 2. Test "Seeking" search (Worker ads)
        Console.WriteLine("\n--- SEARCH RESULTS (SEEKING/WORKER ADS) ---");
        var filtersSeeking = new JobSearchFilters { PostType = PostType.Seeking };
        var resultsSeeking = await searchService.SearchAsync(filtersSeeking);
        PrintResults(resultsSeeking);

        // 3. Test Keyword search
        Console.WriteLine("\n--- SEARCH RESULTS (KEYWORD: 'nấu ăn') ---");
        var filtersKeyword = new JobSearchFilters { Keyword = "nấu ăn" };
        var resultsKeyword = await searchService.SearchAsync(filtersKeyword);
        PrintResults(resultsKeyword);

        // 4. Test Match All (Total raw check)
        Console.WriteLine("\n--- TOTAL DOCUMENTS (MATCH ALL) ---");
        var matchAllResponse = await client.SearchAsync<JobDocument>(s => s.Index("jobs").Query(q => q.MatchAll(new MatchAllQuery())));
        PrintResults(matchAllResponse.Documents);
    }


    private static void PrintResults(IEnumerable<JobDocument> results)
    {
        if (!results.Any())
        {
            Console.WriteLine("No results found.");
            return;
        }

        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        Console.WriteLine(json);
    }
}
