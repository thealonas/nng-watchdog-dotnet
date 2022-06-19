using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using nng.VkFrameworks;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;
using VkNet.Model.RequestParams;

namespace nng_watchdog.API;

public class VkProcessor
{
    private readonly IVkApi _api;

    private readonly ILogger<VkProcessor> _logger;

    private readonly VkFramework _vkFramework;

    private readonly WatchDogApi _watchDogApi;

    public VkProcessor(IVkApi api, ILogger<VkProcessor> logger, WatchDogApi watchDogApi, VkFramework vkFramework)
    {
        _logger = logger;
        _api = api;
        _watchDogApi = watchDogApi;
        _vkFramework = vkFramework;
        _logger.LogInformation("Фреймворк для работы с пользователями инициализирован");
    }


    public async Task<ManagerRole?> GetUserPermissionsAsync(long groupId, long userId)
    {
        while (true)
            try
            {
                var managers = await _api.Groups.GetMembersAsync(new GroupsGetMembersParams
                {
                    GroupId = groupId.ToString(),
                    Filter = GroupsMemberFilters.Managers
                });
                return managers.FirstOrDefault(user => user.Id == userId)?.Role;
            }
            catch (TooManyRequestsException)
            {
                _logger.LogDebug("Слишком частые запросы");
                await Task.Delay(3000);
            }
            catch (Exception e)
            {
                _logger.LogError("{Type}: {Message}", e.GetType(), e.Message);
                return null;
            }
    }

    public void DeletePhoto(ulong photoId, ulong ownerId)
    {
        VkFramework.CaptchaSecondsToWait = 15;
        try
        {
            _vkFramework.DeletePhoto(photoId, ownerId);
        }
        catch (VkApiException e)
        {
            _logger.LogError("{Type}: {Message}", e.GetType(), e.Message);
        }
    }

    public bool FireEditor(long groupId, long userId)
    {
        VkFramework.CaptchaSecondsToWait = 3600;
        try
        {
            _vkFramework.EditManager(userId, groupId, null);
            return true;
        }
        catch (VkApiException e)
        {
            _logger.LogError("{Message}", e.Message);
            return false;
        }
    }

    public bool Block(long groupId, long userId, string comment)
    {
        VkFramework.CaptchaSecondsToWait = 15;
        try
        {
            _vkFramework.Block(groupId, userId, comment);
            return true;
        }
        catch (VkApiException e)
        {
            _logger.LogError("{Type}: {Message}", e.GetType(), e.Message);
            return false;
        }
    }

    public void WallProcessor(long groupId, bool state)
    {
        if (_watchDogApi.GroupAlreadyProcessing(groupId))
        {
            _logger.LogInformation("В сообществе {Group} уже обрабатывается запрос на стену", groupId);
            return;
        }

        _watchDogApi.ChangeWall(groupId, state);
    }

    public void DeletePost(long groupId, long? postId)
    {
        if (postId == null) throw new ArgumentNullException(nameof(postId));
        var post = (long) postId;

        try
        {
            _vkFramework.DeletePost(groupId, post);
        }
        catch (VkApiException e)
        {
            _logger.LogError("Не удалось удалить пост {Post}: {Exception}", post, e.Message);
        }
    }
}
