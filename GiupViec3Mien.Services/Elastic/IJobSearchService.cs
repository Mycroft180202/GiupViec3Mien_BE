using System.Collections.Generic;
using System.Threading.Tasks;
using GiupViec3Mien.Services.DTOs.Job;

namespace GiupViec3Mien.Services.Elastic;

public interface IJobSearchService
{
    Task<IEnumerable<JobDocument>> SearchAsync(JobSearchFilters filters);
    Task InitializeIndexAsync();
    Task ClearIndexAsync();
    Task BulkIndexAsync(IEnumerable<JobDocument> documents);
    Task DeleteAsync(Guid jobId);
}

