using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using nng.Constants;
using nng.Exceptions;
using nng.Helpers;
using nng.VkFrameworks;
using Sentry;
using VkNet.Abstractions;
using VkNet.Enums;

namespace nng_watchdog.API;

public class WatchDogApi
{
    private readonly Dictionary<long, bool> _alreadyProcessingGroups = new();

    private readonly ILogger<WatchDogApi> _logger;
    private readonly VkFramework _vkFramework;
    private readonly VkFrameworkHttp _vkFrameworkHttp;

    public WatchDogApi(ILogger<WatchDogApi> logger, IVkApi vkApi, VkFrameworkHttp vkFrameworkHttp,
        VkFramework vkFramework)
    {
        _logger = logger;
        _vkFrameworkHttp = vkFrameworkHttp;
        _vkFramework = vkFramework;

        var groupToken = EnvironmentHelper.GetString(EnvironmentConstants.DialogGroupToken);
        if (groupToken == null)
            throw new ArgumentNullException(nameof(groupToken), "Не задан ключ для логирования в группу");

        var user = vkApi.UserId;
        if (user == null) throw new ArgumentNullException(nameof(user), "Не удалось подключиться к VK API");

        Owner = EnvironmentHelper.GetLong(EnvironmentConstants.LogUser);

        logger.LogInformation("Авторизация от имени @id{Id}", user);
        logger.LogInformation("Логгирование будет происходить в @id{Id}", user);
    }

    private long Owner { get; }

    public bool GroupAlreadyProcessing(long group)
    {
        return _alreadyProcessingGroups.ContainsKey(group) && _alreadyProcessingGroups[group];
    }

    public void SendMessage(string message)
    {
        try
        {
            _vkFrameworkHttp.SendMessage(message, null, Owner, true);
        }
        catch (VkFrameworkMethodException e)
        {
            SentrySdk.CaptureException(e);
            _logger.LogError("Не удалось отправить сообщение: {Message}\n{Exception}", message,
                $"{e.GetType()}: {e.Message}");
        }
    }

    public void ChangeWall(long group, bool state)
    {
        SetGroupProcessingStatus(group, true);
        _vkFramework.SetWall(group, state ? WallContentAccess.Restricted : WallContentAccess.Off);
        _logger.LogInformation("Стена группы {Group} изменена на {State}", group, state);
        SetGroupProcessingStatus(group, false);
    }

    private void SetGroupProcessingStatus(long group, bool state)
    {
        if (!_alreadyProcessingGroups.ContainsKey(group)) _alreadyProcessingGroups.Add(group, state);
        else _alreadyProcessingGroups[group] = state;
    }
}
