using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace nng_watchdog;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = OperatingSystem.IsWindows() ? Encoding.Unicode : Encoding.UTF8;
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        var config = new ConfigurationBuilder().AddJsonFile("Configs/appsettings.json").Build();
        var configs = new[]
        {
            "appsettings.json",
            "phrases.json",
            "groups.json"
        };

        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.SetBasePath(Directory.GetCurrentDirectory());

                foreach (var path in configs) configuration.AddJsonFile($"Configs/{path}", false, true);

                configuration.AddEnvironmentVariables();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel();

                webBuilder.UseSentry(o => { o.Environment = config["Sentry:Environment"]; });

                webBuilder.UseStartup<Startup>();
            });
    }
}
