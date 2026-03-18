using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace GiupViec3Mien.Services.BackgroundJobs;

public static class HangfireJobRegistrar
{
    public static void RegisterJobs(IServiceProvider serviceProvider)
    {
        var recurringJobManager = serviceProvider.GetRequiredService<IRecurringJobManager>();
        var jobs = serviceProvider.GetServices<IBackgroundJob>();

        foreach (var job in jobs)
        {
            recurringJobManager.AddOrUpdate(
                job.JobId, 
                () => job.ExecuteAsync(), 
                job.CronExpression
            );
        }
    }
}
