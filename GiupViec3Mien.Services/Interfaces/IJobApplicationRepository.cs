using GiupViec3Mien.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface IJobApplicationRepository
{
    Task AddAsync(JobApplication application);
    Task<JobApplication?> GetByIdAsync(Guid id);
    Task<IEnumerable<JobApplication>> GetByJobIdAsync(Guid jobId);
    Task<IEnumerable<JobApplication>> GetByApplicantIdAsync(Guid applicantId);
    Task<JobApplication?> GetByApplicantAndJobAsync(Guid applicantId, Guid jobId);
    Task SaveChangesAsync();
}
