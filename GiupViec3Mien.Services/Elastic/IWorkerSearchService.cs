using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GiupViec3Mien.Services.DTOs.User;

namespace GiupViec3Mien.Services.Elastic;

public interface IWorkerSearchService
{
    Task<WorkerSearchResult> SearchAsync(WorkerSearchFilters filters);

    Task InitializeIndexAsync();
    Task BulkIndexAsync(IEnumerable<WorkerDocument> documents);
    Task UpdateAsync(WorkerDocument document);
    Task DeleteAsync(Guid userId);
}
