using Microsoft.Extensions.Logging;
using nng_watchdog.Models;
using nng.DatabaseProviders;
using Redis.OM;

namespace nng_watchdog.Providers;

public class GroupSecretsProvider : DatabaseProvider<GroupSecret>
{
    public GroupSecretsProvider(ILogger<DatabaseProvider<GroupSecret>> logger, RedisConnectionProvider provider)
        : base(logger, provider)
    {
    }
}
