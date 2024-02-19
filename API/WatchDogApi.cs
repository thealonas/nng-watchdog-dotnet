using System;
using Microsoft.Extensions.Logging;
using nng_watchdog.Providers;
using nng.DatabaseProviders;
using nng.Exceptions;
using nng.Extensions;
using nng.VkFrameworks;
using Sentry;

namespace nng_watchdog.API;

public class WatchDogApi
{
    private readonly ILogger<WatchDogApi> _logger;
    private readonly VkFrameworkHttp _vk;

    public WatchDogApi(ILogger<WatchDogApi> logger, SettingsDatabaseProvider settingsDatabaseProvider,
        WatchdogSettingsProvider watchDogDatabaseProvider)
    {
        _logger = logger;

        if (!settingsDatabaseProvider.Collection.TryGetById("main", out var settings))
            throw new ArgumentException(null, nameof(settingsDatabaseProvider));

        if (!watchDogDatabaseProvider.Collection.TryGetById("main", out var watchDogSettings))
            throw new ArgumentException(null, nameof(watchDogDatabaseProvider));

        var groupToken = watchDogSettings.GroupToken;
        _vk = new VkFrameworkHttp(groupToken);

        var user = settings.LogUser;

        Owner = settings.LogUser;

        logger.LogInformation("Авторизация от имени @id{Id}", user);
        logger.LogInformation("Логгирование будет происходить в @id{Id}", user);
    }

    private long Owner { get; }

    public void SendMessage(string message)
    {
        try
        {
            _vk.SendMessage(message, null, Owner, true);
        }
        catch (VkFrameworkMethodException e)
        {
            SentrySdk.CaptureException(e);
            _logger.LogError("Не удалось отправить сообщение: {Message}\n{Exception}", message,
                $"{e.GetType()}: {e.Message}");
        }
    }
}
