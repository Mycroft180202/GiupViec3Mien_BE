using System.Threading.Tasks;

namespace GiupViec3Mien.Services.BackgroundJobs;

public interface IBackgroundJob
{
    string JobId { get; }
    string CronExpression { get; }
    Task ExecuteAsync();
}
