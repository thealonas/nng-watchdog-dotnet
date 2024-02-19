using nng_watchdog.API;
using nng.DatabaseProviders;
using nng.Extensions;
using nng.VkFrameworks;

namespace nng_watchdog.Providers;

public class VkProvider
{
    public VkProvider(TokensDatabaseProvider tokensDatabaseProvider)
    {
        var token = tokensDatabaseProvider.GetTokenWithPermission("watchdog").Token;
        VkProcessor = new VkProcessor(new VkFramework(token));
    }

    public VkProcessor VkProcessor { get; }
}
