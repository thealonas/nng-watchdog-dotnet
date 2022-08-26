using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using nng.VkFrameworks;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.Attachments;
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
    }


    public ManagerRole? GetUserPermissions(long groupId, long userId)
    {
        return VkFrameworkExecution.ExecuteWithReturn(() =>
        {
            var managers = _api.Groups.GetMembers(new GroupsGetMembersParams
            {
                GroupId = groupId.ToString(),
                Filter = GroupsMemberFilters.Managers
            });
            return managers.FirstOrDefault(user => user.Id == userId)?.Role;
        });
    }

    public void DeletePhoto(ulong photoId, long ownerId)
    {
        VkFramework.CaptchaSecondsToWait = 15;
        VkFrameworkExecution.Execute(() => { _vkFramework.Api.Photo.Delete(photoId, ownerId); });
    }

    public bool TryFireEditor(long groupId, long userId)
    {
        VkFramework.CaptchaSecondsToWait = 3600;
        _vkFramework.EditManager(userId, groupId, null);
        return true;
    }

    public bool Block(long groupId, long userId, string comment)
    {
        VkFramework.CaptchaSecondsToWait = 15;
        _vkFramework.Block(groupId, userId, comment);
        return true;
    }

    public void WallProcessor(long groupId, bool state)
    {
        if (_watchDogApi.GroupAlreadyProcessing(groupId))
        {
            _logger.LogWarning("В сообществе {Group} уже обрабатывается запрос на стену", groupId);
            return;
        }

        _watchDogApi.ChangeWall(groupId, state);
    }

    public void DeletePost(long groupId, long? postId)
    {
        if (postId == null) throw new ArgumentNullException(nameof(postId));
        var post = (long) postId;
        _vkFramework.DeletePost(groupId, post);
    }

    public IEnumerable<Photo> GetPhotos(long owner, PhotoAlbumType type)
    {
        var photos = VkFrameworkExecution.ExecuteWithReturn(() => _vkFramework.Api.Photo.Get(new PhotoGetParams
        {
            OwnerId = owner,
            AlbumId = type,
            Count = 1000
        }));

        if (photos.TotalCount <= 1000) return photos.ToList();

        var divisor = (int) Math.Ceiling(photos.TotalCount / 1000f);

        var output = photos.ToList();

        for (var i = 1; i < divisor; i++)
            output.AddRange(VkFrameworkExecution.ExecuteWithReturn(() => _vkFramework.Api.Photo.Get(new PhotoGetParams
            {
                OwnerId = owner,
                AlbumId = type,
                Count = 1000,
                Offset = (ulong) (i * 1000)
            })));

        return output;
    }
}
