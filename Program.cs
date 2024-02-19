using System;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        return Host.CreateDefaultBuilder(args)
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
                    o.Environment = "dev";
                });

                webBuilder.UseStartup<Startup>();
            });
    }
}
