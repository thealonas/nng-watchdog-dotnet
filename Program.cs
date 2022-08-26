using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nng.Constants;
using nng.Helpers;
using Sentry;
using Sentry.Extensibility;

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
        var configs = new[]
        {
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
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel();

                webBuilder.UseUrls("http://*:1221");

                webBuilder.UseSentry(o =>
                {
                    o.Dsn = "https://9912af92176d4893a7dd0139fd9ad933@o555933.ingest.sentry.io/6173973";
                    o.MaxRequestBodySize = RequestSize.Always;
                    o.SendDefaultPii = true;
                    o.MinimumBreadcrumbLevel = LogLevel.Debug;
                    o.MinimumEventLevel = LogLevel.Warning;
                    o.AttachStacktrace = true;
                    o.Debug = false;
                    o.DiagnosticLevel = SentryLevel.Error;

                    var targetEnv = EnvironmentHelper.GetString(EnvironmentConstants.Sentry, "prod");
                    o.Environment = targetEnv;
                });

                webBuilder.UseStartup<Startup>();
            });
    }
}
