using GiupViec3Mien.Services.Elastic;
using GiupViec3Mien.Services.Messaging;
using MassTransit;
using Elastic.Clients.Elasticsearch;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace GiupViec3Mien.Services.Messaging.Consumers;

public class JobElasticSyncConsumer : IConsumer<JobIndexMessage>, IConsumer<JobDeleteMessage>
{
    private readonly ElasticsearchClient _elasticClient;
    private const string IndexName = "jobs";

    public JobElasticSyncConsumer(ElasticsearchClient elasticClient)
    {
        _elasticClient = elasticClient;
    }

    public async Task Consume(ConsumeContext<JobIndexMessage> context)
    {
        var msg = context.Message;
        
        // Deserialize Skills (they come as a JSON string from DB)
        List<string> skills = new();
        if (!string.IsNullOrEmpty(msg.RequiredSkills))
        {
            try {
                skills = JsonSerializer.Deserialize<List<string>>(msg.RequiredSkills) ?? new();
            } catch { /* Handle gracefully */ }
        }

        var doc = new JobDocument
        {
            Id = msg.JobId.ToString(),
            Title = msg.Title,
            Description = msg.Description,
            Category = msg.Category,
            Price = msg.Price,
            Coordinates = new global::Elastic.Clients.Elasticsearch.Location(msg.Lat, msg.Lon),
            RequiredSkills = skills,
            Status = msg.Status,
            PostType = msg.PostType,
            CreatedAt = msg.CreatedAt,
            EmployerId = msg.EmployerId,
            EmployerName = msg.EmployerName,
            EmployerAvatarUrl = msg.EmployerAvatarUrl,
            ApplicantCount = msg.ApplicantCount
        };

        // Create or update index
        await _elasticClient.IndexAsync(doc, idx => idx.Index(IndexName));
    }

    public async Task Consume(ConsumeContext<JobDeleteMessage> context)
    {
        var msg = context.Message;
        await _elasticClient.DeleteAsync<JobDocument>(msg.JobId.ToString(), d => d.Index(IndexName));
    }
}
