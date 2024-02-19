using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using nng_watchdog.API;
using nng_watchdog.BackgroundTasks;
using nng_watchdog.Helpers;
using nng_watchdog.Providers;
using nng.DatabaseProviders;
using nng.Helpers;
using Redis.OM;

namespace nng_watchdog;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc(options => options.EnableEndpointRouting = false);
        services.AddMvc().AddNewtonsoftJson();

        services.AddHttpClient();

        var connection = new RedisConnectionProvider(EnvironmentHelper.GetString("REDIS_URL"));

        services.AddSingleton(connection);
        services.AddSingleton<GroupSecretsProvider>();
        services.AddSingleton<GroupsDatabaseProvider>();
        services.AddSingleton<SettingsDatabaseProvider>();
        services.AddSingleton<TokensDatabaseProvider>();
        services.AddSingleton<WatchdogSettingsProvider>();
        services.AddSingleton<UsersDatabaseProvider>();

        services.AddSingleton<VkProvider>();

        services.AddSingleton<WatchDogApi>();

        services.AddSingleton<PhotoHelper>();
        services.AddSingleton<UsersHelper>();

        services.AddHostedService<SecretsUpdater>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseAuthentication();

        app.UseMvc();

        app.UseRouting();
    }
}
