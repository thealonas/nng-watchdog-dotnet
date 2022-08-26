using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using nng_watchdog.API;
using nng.Constants;
using nng.Helpers;
using nng.VkFrameworks;
using VkNet;
using VkNet.Abstractions;
using VkNet.Model;

namespace nng_watchdog;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc(options => options.EnableEndpointRouting = false);
        services.AddMvc().AddNewtonsoftJson();
        services.AddSingleton<VkProcessor>();
        services.AddSingleton<IVkApi>(_ =>
        {
            var api = new VkApi();
            api.Authorize(new ApiAuthParams
                {AccessToken = EnvironmentHelper.GetString(EnvironmentConstants.UserToken)});
            api.RequestsPerSecond = 1;
            api.UserId = api.Users.Get(new List<long>()).First().Id;
            return api;
        });
        services.AddSingleton(_ => new VkFramework(EnvironmentHelper.GetString(EnvironmentConstants.UserToken)));
        services.AddSingleton(_ =>
            new VkFrameworkHttp(EnvironmentHelper.GetString(EnvironmentConstants.DialogGroupToken)));
        services.AddSingleton<WatchDogApi>();
        services.AddHttpClient();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseAuthentication();

        app.UseMvc();

        app.UseRouting();
    }
}
