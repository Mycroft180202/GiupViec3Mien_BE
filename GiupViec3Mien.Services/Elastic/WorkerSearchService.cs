using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using GiupViec3Mien.Services.DTOs.User;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Elastic;

public record WorkerSearchResult(IEnumerable<WorkerDocument> Results, long Total);


public class WorkerSearchService : IWorkerSearchService
{
    private readonly ElasticsearchClient _client;
    private const string IndexName = "workers";

    public WorkerSearchService(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task InitializeIndexAsync()
    {
        var exists = await _client.Indices.ExistsAsync(IndexName);
        if (exists.Exists) await _client.Indices.DeleteAsync(IndexName);

        await _client.Indices.CreateAsync(IndexName, c => c
            .Mappings(m => m
                .Properties<WorkerDocument>(p => p
                    .Keyword(n => n.Id)
                    .Text(n => n.FullName)
                    .Text(n => n.Bio)
                    .DoubleNumber(n => n.HourlyRate)
                    .Keyword(n => n.Skills)
                    .Text(n => n.Location)
                    .Boolean(n => n.Verified)
                    .IntegerNumber(n => n.ExperienceYears)
                    .GeoPoint(n => n.Coordinates)

                    .Keyword(n => n.ServiceCategories)
                    .Keyword(n => n.ServiceCategory)
                )

            )
        );
    }

    public async Task<WorkerSearchResult> SearchAsync(WorkerSearchFilters filters)

    {
        var response = await _client.SearchAsync<WorkerDocument>(s => s
            .Index(IndexName)
            .From((filters.Page - 1) * filters.PageSize)
            .Size(filters.PageSize)
            .Query(q => q
                .Bool(b => {
                    var must = new List<Query>();
                    var filterList = new List<Query>();

                    if (!string.IsNullOrEmpty(filters.Keyword))
                    {
                        must.Add(new MultiMatchQuery
                        {
                            Fields = new[] { "fullName", "bio", "skills", "location" },
                            Query = filters.Keyword,
                            Fuzziness = new Fuzziness("auto")
                        });


                    }
                    else { must.Add(new MatchAllQuery()); }

                    if (!string.IsNullOrEmpty(filters.Location))
                        filterList.Add(new MatchQuery(new Field("location")) { Query = filters.Location });
                    
                    if (!string.IsNullOrEmpty(filters.Category))
                    {
                        // In this project cat IDs (0, 1, 2, 3) are mapped to names. Match against both if possible.
                        filterList.Add(new MatchQuery(new Field("serviceCategory")) { Query = filters.Category });
                    }

                    if (!string.IsNullOrEmpty(filters.Timing))
                    {
                        var timingLabel = filters.Timing == "0" ? "fulltime" : "parttime";
                        filterList.Add(new TermQuery(new Field("timingType")) { Value = timingLabel });
                    }

                    if (filters.VerifiedOnly == true)
                        filterList.Add(new TermQuery(new Field("verified")) { Value = true });


                    if (filters.MinRate.HasValue || filters.MaxRate.HasValue)
                    {
                        filterList.Add(new NumberRangeQuery(new Field("hourlyRate")) 
                        { 
                            Gte = (double?)filters.MinRate, 
                            Lte = (double?)filters.MaxRate 
                        });
                    }

                    if (filters.MinExpYears.HasValue)
                    {
                        filterList.Add(new NumberRangeQuery(new Field("experienceYears")) { Gte = filters.MinExpYears.Value });
                    }

                    if (must.Any()) b.Must(must.ToArray());
                    if (filterList.Any()) b.Filter(filterList.ToArray());
                })
            )
        );


        return new WorkerSearchResult(response.Documents, response.Total);
    }


    public async Task BulkIndexAsync(IEnumerable<WorkerDocument> documents)
    {
        if (documents == null || !documents.Any()) return;
        var response = await _client.BulkAsync(b => b.Index(IndexName).IndexMany(documents));
        if (!response.IsValidResponse)
        {
            Console.WriteLine($"Bulk indexing failed! Debug: {response.DebugInformation}");
            if (response.ItemsWithErrors.Any())
            {
                foreach(var item in response.ItemsWithErrors)
                {
                    Console.WriteLine($"Item Error: {item.Error?.Reason}");
                }
            }
            throw new Exception($"Bulk indexing failed: {response.DebugInformation}");
        }
    }



    public async Task UpdateAsync(WorkerDocument document)
    {
        await _client.IndexAsync(document, index: IndexName);
    }


    public async Task DeleteAsync(Guid userId)
    {
        await _client.DeleteAsync<WorkerDocument>(userId.ToString(), d => d.Index(IndexName));
    }
}
