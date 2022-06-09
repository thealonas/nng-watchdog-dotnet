using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nng.VkFrameworks;
using nng_watchdog.API;
using VkNet;
using VkNet.Abstractions;
using VkNet.Model;

namespace nng_watchdog;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc(options => options.EnableEndpointRouting = false);
        services.AddMvc().AddNewtonsoftJson();
        services.AddSingleton<VkProcessor>();
        services.AddSingleton<IVkApi>(_ =>
        {
            var api = new VkApi();
            api.Authorize(new ApiAuthParams {AccessToken = Configuration.GetSection("Data:Token").Value});
            api.RequestsPerSecond = 1;
            api.UserId = api.Users.Get(new List<long>()).First().Id;
            return api;
        });
        services.AddSingleton(_ => new VkFramework(Configuration.GetSection("Data:Token").Value));
        services.AddSingleton(_ => new VkFrameworkHttp(Configuration.GetSection("Data:LogGroupToken").Value));
        services.AddSingleton<WatchDogApi>();
        services.AddSingleton<PhraseProcessor>();
        services.AddHttpClient();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseAuthentication();

        app.UseMvc();

        app.UseRouting();
    }
}
