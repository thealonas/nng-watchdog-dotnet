using Microsoft.Extensions.Logging;
using nng_watchdog.Models;
using nng.DatabaseProviders;
using Redis.OM;

namespace nng_watchdog.Providers;

public class WatchdogSettingsProvider : DatabaseProvider<WatchdogSettings>
{
    public WatchdogSettingsProvider(ILogger<DatabaseProvider<WatchdogSettings>> logger,
        RedisConnectionProvider provider)
        : base(logger, provider)
    {
    }
}
