using System;
using System.Collections.Generic;
using Elastic.Clients.Elasticsearch;
using ES = global::Elastic.Clients.Elasticsearch;


namespace GiupViec3Mien.Services.Elastic;

public class JobDocument
{
    public string Id { get; set; } = string.Empty; // Must be string for ES
    
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    
    // Elasticsearch Geo-point structure
    public JobGeoPoint Coordinates { get; set; }






    
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PostType { get; set; } = string.Empty;
    
    public List<string> RequiredSkills { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }

    // For fast retrieval without hitting DB
    public Guid EmployerId { get; set; }
    public string EmployerName { get; set; } = string.Empty;
    public string? EmployerAvatarUrl { get; set; }
    public int ApplicantCount { get; set; }
}
