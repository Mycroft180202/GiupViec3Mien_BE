using System;
using System.Threading;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface IDistributedLockService
{
    Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
}
