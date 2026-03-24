using System;
using System.Threading;
using System.Threading.Tasks;
using GiupViec3Mien.Services.Interfaces;
using StackExchange.Redis;

namespace GiupViec3Mien.Services.Infrastructure;

public class RedisDistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisDistributedLockService(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var token = Guid.NewGuid().ToString("N");
        var acquired = await db.StringSetAsync(key, token, expiry, When.NotExists);

        if (!acquired)
        {
            return null;
        }

        return new RedisLockHandle(db, key, token);
    }

    private sealed class RedisLockHandle : IAsyncDisposable
    {
        private const string ReleaseScript =
            """
            if redis.call("get", KEYS[1]) == ARGV[1] then
                return redis.call("del", KEYS[1])
            end
            return 0
            """;

        private readonly IDatabase _database;
        private readonly string _key;
        private readonly string _token;

        public RedisLockHandle(IDatabase database, string key, string token)
        {
            _database = database;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            await _database.ScriptEvaluateAsync(
                ReleaseScript,
                new RedisKey[] { _key },
                new RedisValue[] { _token });
        }
    }
}
