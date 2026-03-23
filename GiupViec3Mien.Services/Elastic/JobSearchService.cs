using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using GiupViec3Mien.Services.DTOs.Job;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace GiupViec3Mien.Services.Elastic;

public class JobSearchService : IJobSearchService
{
    private readonly ElasticsearchClient _client;
    private const string IndexName = "jobs";

    public JobSearchService(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task<IEnumerable<JobDocument>> SearchAsync(JobSearchFilters filters)
    {
        Console.WriteLine($"Search Request: Keyword='{filters.Keyword}', Category={filters.Category}, PostType={filters.PostType}");

        var response = await _client.SearchAsync<JobDocument>(s => s
            .Indices(IndexName)
            .From(0)
            .Size(1000)
            .Query(q => q
                .Bool(b => {
                    var must = new List<Query>();
                    var filterList = new List<Query>();

                    if (!string.IsNullOrEmpty(filters.Keyword))
                    {
                        must.Add(new MultiMatchQuery
                        {
                            Fields = new[] { "title", "description" },
                            Query = filters.Keyword
                        });
                    }
                    else { must.Add(new MatchAllQuery()); }

                    // We now use Term queries on exact keyword fields
                    if (filters.Category.HasValue)
                        filterList.Add(new TermQuery(new Field("category")) { Value = filters.Category.Value.ToString().ToLowerInvariant() });

                    if (filters.PostType.HasValue)
                        filterList.Add(new TermQuery(new Field("postType")) { Value = filters.PostType.Value.ToString().ToLowerInvariant() });

                    // Only show Open jobs
                    filterList.Add(new TermQuery(new Field("status")) { Value = "open" });

                    if (must.Any()) b.Must(must);
                    if (filterList.Any()) b.Filter(filterList);
                })
            )
            .Sort(srt => srt.Field(f => f.CreatedAt, f => f.Order(SortOrder.Desc)))
        );

        if (!response.IsSuccess())
        {
            Console.WriteLine($"Search Failed: {response.DebugInformation}");
            return Enumerable.Empty<JobDocument>();
        }

        Console.WriteLine($"Found {response.Documents.Count} matches.");
        return response.Documents;
    }

    public async Task InitializeIndexAsync()
    {
        await ClearIndexAsync();
        
        // Use v8 compatible explicit mappings for exactly what we need
        await _client.Indices.CreateAsync(IndexName, c => c
            .Mappings(m => m
                .Properties<JobDocument>(p => p
                    .Text(f => f.Title)
                    .Text(f => f.Description)
                    .Keyword(f => f.Category)
                    .Keyword(f => f.PostType)
                    .Keyword(f => f.Status)
                    .Date(f => f.CreatedAt)
                    .GeoPoint(f => f.Coordinates)
                )
            )
        );
    }

    public async Task ClearIndexAsync()
    {
        var exists = await _client.Indices.ExistsAsync(IndexName);
        if (exists.Exists) await _client.Indices.DeleteAsync(IndexName);
    }

    public async Task BulkIndexAsync(IEnumerable<JobDocument> documents)
    {
        foreach (var doc in documents)
        {
            // IMPORTANT: Setting Id ensures we overwrite existing docs instead of duplicating them
            var r = await _client.IndexAsync(doc, idx => idx.Index(IndexName).Id(doc.Id));
            if (!r.IsSuccess()) Console.WriteLine($"Indexing failed for document {doc.Id}: {r.DebugInformation}");
        }
    }

    public async Task DeleteAsync(Guid jobId)
    {
        await _client.DeleteAsync<JobDocument>(jobId.ToString(), d => d.Index(IndexName));
    }
}
