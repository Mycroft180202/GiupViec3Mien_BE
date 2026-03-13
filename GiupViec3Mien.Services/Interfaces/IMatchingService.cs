using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GiupViec3Mien.Services.DTOs.Matching;

namespace GiupViec3Mien.Services.Interfaces;

public interface IMatchingService
{
    Task<List<MatchResultDto>> GetBestMatchesForJobAsync(Guid jobId, int limit = 10);
    Task<List<MatchResultDto>> GetBestMatchesForEmployerAsync(Guid employerId, int limit = 10);
    Task<double> CalculateDistanceAsync(Guid userId, Guid jobId);
    Task<double> GetUserRatingAsync(Guid userId);
    Task<List<string>> GetSkillMatchAsync(Guid userId, Guid jobId);
    Task<EmployerExperienceDto> GetEmployerExperienceAsync(Guid employerId);
    Task<double> GetBudgetFitScoreAsync(Guid userId, Guid jobId);
}
