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
        var response = await _client.SearchAsync<JobDocument>(s => s
            .Index(IndexName)
            .From(0)
            .Size(50) // Default page size
            .Query(q => q
                .Bool(b => b
                    .Must(m => {
                        if (!string.IsNullOrEmpty(filters.Keyword))
                            m.MultiMatch(mm => mm
                                .Fields(new[] { "title^3", "description" }) // Boost Title
                                .Query(filters.Keyword)
                                .Fuzziness(new Fuzziness("AUTO")));
                    })
                    .Filter(f => {
                        // Category Filter
                        if (filters.Category.HasValue)
                            f.Term(t => t.Field(fld => fld.Category).Value(filters.Category.Value.ToString()));
                        
                        // Price Range
                        if (filters.MinPrice.HasValue || filters.MaxPrice.HasValue)
                            f.Range(r => r.NumberRange(nr => nr
                                .Field(fld => fld.Price)
                                .Gte((double?)filters.MinPrice)
                                .Lte((double?)filters.MaxPrice)));

                        // Post Type (Seeking vs Hiring)
                        f.Term(t => t.Field(fld => fld.PostType).Value(filters.PostType.ToString()));

                        // Geo Proximity Filter
                        if (filters.Latitude.HasValue && filters.Longitude.HasValue && filters.RadiusKm.HasValue)
                        {
                            f.GeoDistance(gd => gd
                                .Field(fld => fld.Coordinates)
                                .Distance($"{filters.RadiusKm.Value}km")
                                .Location(new Location(filters.Latitude.Value, filters.Longitude.Value)));
                        }
                    })
                )
            )
            .Sort(srt => {
                // If user provided location, sort by nearest
                if (filters.Latitude.HasValue && filters.Longitude.HasValue)
                {
                    srt.GeoDistance(gd => gd
                        .Field(f => f.Coordinates)
                        .Location(new Location(filters.Latitude.Value, filters.Longitude.Value))
                        .Order(SortOrder.Asc));
                }
                else
                {
                    // Otherwise sort by recency
                    srt.Field(f => f.CreatedAt, f => f.Order(SortOrder.Desc));
                }
            })
        );

        if (!response.IsSuccess())
        {
            // Log error or handle failure
            return Enumerable.Empty<JobDocument>();
        }

        return response.Documents;
    public async Task InitializeIndexAsync()
    {
        await ClearIndexAsync();

        await _client.Indices.CreateAsync(IndexName, i => i
            .Mappings(m => m
                .Properties<JobDocument>(p => p
                    .Text(t => t.Title) // Analyzer support can be added if Elastic has vi plugin
                    .Text(t => t.Description)
                    .Keyword(k => k.Category)
                    .Keyword(k => k.RequiredSkills)
                    .Keyword(k => k.Status)
                    .Keyword(k => k.PostType)
                    .GeoPoint(g => g.Coordinates)
                    .Double(d => d.Price)
                    .Date(d => d.CreatedAt)
                )
            )
        );
    }

    public async Task ClearIndexAsync()
    {
        var exists = await _client.Indices.ExistsAsync(IndexName);
        if (exists.Exists)
        {
            await _client.Indices.DeleteAsync(IndexName);
        }
    }

    public async Task BulkIndexAsync(IEnumerable<JobDocument> documents)
    {
        await _client.IndexManyAsync(documents, IndexName);
    }
}
