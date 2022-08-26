using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using nng_watchdog.API;
using nng.Helpers;
using Sentry;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;
using VkNet.Model.Attachments;
using VkNet.Model.GroupUpdate;
using VkNet.Utils;
using Event = nng_watchdog.Models.Event;

namespace nng_watchdog.Controllers;

[Route("")]
[ApiController]
public class WatchdogController : ControllerBase
{
    private readonly ILogger<WatchdogController> _logger;

    public WatchdogController(IVkApi vkApi, VkProcessor managerApi, WatchDogApi watchDogApi,
        IConfiguration configuration, ILogger<WatchdogController> logger)
    {
        _logger = logger;

        Configuration = configuration;
        VkApi = vkApi;
        ManagerApi = managerApi;
        WatchDog = watchDogApi;
    }

    private IConfiguration Configuration { get; }

    private IVkApi VkApi { get; }

    private WatchDogApi WatchDog { get; }

    private VkProcessor ManagerApi { get; }

    private bool OperationAllowed(long group, long user)
    {
        ManagerRole? perms;
        try
        {
            perms = ManagerApi.GetUserPermissions(group, user);
        }
        catch (VkApiException e)
        {
            _logger.LogError("{Type}: {Message}", e.GetType(), e.Message);
            perms = null;
        }

        _logger.LogDebug("Права пользователя {User} в сообществе {Group} — {Perms}",
            user, group, perms?.ToString());
        return perms == ManagerRole.Administrator || perms == ManagerRole.Creator;
    }

    private string FireEditorWithResponse(long group, long user)
    {
        var fireState = ManagerApi.TryFireEditor(group, user);
        var banState = ManagerApi.Block(group, user, EnvironmentHelper.GetString("BanComment"));

        return PhraseProcessor.FiredEditor(group, user.ToString(), fireState)
               + Environment.NewLine
               + PhraseProcessor.BannedEditor(group, user.ToString(), banState);
    }

    [HttpPost]
    public IActionResult Callback([FromBody] Event vkEvent)
    {
        _logger.LogInformation("Получили ивент от VK");
        _logger.LogInformation("Сообщество: {Group}\n\tТип: {Type}",
            vkEvent.GroupId, vkEvent.Type);

        string? secret;
        try
        {
            secret = Configuration[$"{vkEvent.GroupId}:Secret"];
        }
        catch (Exception e)
        {
            _logger.LogWarning("Не удалось найти заявленное сообщество ({Group})\n{Exception}",
                vkEvent.GroupId, $"{e.GetType()}: {e.Message}");
            return Ok("ok");
        }

        if (vkEvent.Secret != secret)
        {
            _logger.LogWarning("У запроса с типом {Type} неправильный secret ({ActualSecret})",
                vkEvent.Type,
                vkEvent.Secret);
            return Ok("ok");
        }

        if (vkEvent.Type == "confirmation")
        {
            var group = vkEvent.GroupId;
            var token = Configuration.GetSection($"{group}:Confirm").Value;
            _logger.LogInformation("Пришел запрос на подтверждение сервера в сообществе {GroupId}" +
                                   "\nВозвращаем {Token}", group, token);
            return Ok(token);
        }

        Task.Run(() =>
        {
            try
            {
                ProcessEvent(vkEvent);
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
            }
        });

        return Ok("ok");
    }

    private void ProcessEvent(Event vkEvent)
    {
        switch (vkEvent.Type)
        {
            case "photo_new":
                ProcessPhoto(vkEvent);
                break;

            case "wall_repost":
            case "wall_post_new":
                // TODO: issue #16
                ProcessWallTemp(vkEvent);
                break;

            case "user_block":
                ProcessUserBlock(vkEvent);
                break;

            case "user_unblock":
                ProcessUserUnBlock(vkEvent);
                break;

            case "group_leave":
                ProcessGroupLeave(vkEvent);
                break;

            case "group_change_photo":
                ProcessChangePhoto(vkEvent);
                break;

            default:
                _logger.LogWarning("Неизвестный тип запроса");
                break;
        }
    }

    private void ProcessPhoto(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var photo = Photo.FromJson(new VkResponse(vkEvent.Object));
        if (photo.UserId == null || photo.UserId == VkApi.UserId) return;
        if (OperationAllowed(group, (long) photo.UserId)) return;

        var response = PhraseProcessor.PhotoNew(group, photo.OwnerId.ToString() ?? throw new NullReferenceException());

        WatchDog.SendMessage(response);
    }

    [Obsolete("Будем использовать после того, как ВК пофиксит стену (issue #16)", true)]
    private void ProcessWall(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var post = Post.FromJson(new VkResponse(vkEvent.Object));
        if (post.CreatedBy == null || post.Id == null || post.CreatedBy == VkApi.UserId) return;
        if (OperationAllowed(group, (long) post.CreatedBy)) return;
        var response = PhraseProcessor.WallPost(group, post.CreatedBy.ToString() ?? throw new NullReferenceException());

        ManagerApi.WallProcessor(group, true);
        ManagerApi.DeletePost(group, post.Id);
        ManagerApi.WallProcessor(group, false);

        response += Environment.NewLine;
        response += FireEditorWithResponse(group, (long) post.CreatedBy);

        WatchDog.SendMessage(response);
    }

    private void ProcessWallTemp(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var response = PhraseProcessor.WallPostTemp(group);
        WatchDog.SendMessage(response);
    }

    private void ProcessUserBlock(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var block = UserBlock.FromJson(new VkResponse(vkEvent.Object));
        if (block.UserId == null || block.AdminId == null || block.AdminId == VkApi.UserId) return;
        if (OperationAllowed(group, (long) block.AdminId)) return;

        var response = PhraseProcessor.UserBlock(group, block.AdminId.ToString()!, block.UserId.ToString()!,
            block.Comment);

        response += Environment.NewLine;
        response += FireEditorWithResponse(group, (long) block.AdminId);

        WatchDog.SendMessage(response);
    }

    private void ProcessUserUnBlock(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var unblock = UserUnblock.FromJson(new VkResponse(vkEvent.Object));
        if (unblock.UserId == null || unblock.AdminId == null ||
            Equals(unblock.AdminId, VkApi.UserId)) return;
        if (OperationAllowed(group, (long) unblock.AdminId)) return;
        if (unblock.ByEndDate is true)
        {
            var user = (long) unblock.UserId;
            var dateTimeResponse = PhraseProcessor.UserDateTime(group, user.ToString());
            ManagerApi.Block(group, user, EnvironmentHelper.GetString("BanComment"));
            dateTimeResponse += PhraseProcessor.BannedAgain(group, user.ToString());
            WatchDog.SendMessage(dateTimeResponse);
            return;
        }

        var response = PhraseProcessor.UserUnblock(group, unblock.AdminId.ToString()!, unblock.UserId.ToString()!);

        response += Environment.NewLine +
                    FireEditorWithResponse(group, (long) unblock.AdminId);

        ManagerApi.Block(group, (long) unblock.UserId, EnvironmentHelper.GetString("BanComment"));

        response += Environment.NewLine + PhraseProcessor.BannedAgain(group, unblock.UserId.ToString()!);

        WatchDog.SendMessage(response);
    }

    private void ProcessGroupLeave(Event vkEvent)
    {
        var leave = GroupLeave.FromJson(new VkResponse(vkEvent.Object));
        if (leave.UserId == null || leave.IsSelf == null || (bool) leave.IsSelf || leave.UserId == VkApi.UserId)
            return;
        var response = PhraseProcessor.GroupLeave(vkEvent.GroupId, leave.UserId.ToString()!);
        WatchDog.SendMessage(response);
    }

    private void ProcessChangePhoto(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var groupChangePhoto = GroupChangePhoto.FromJson(new VkResponse(vkEvent.Object));
        if (groupChangePhoto.UserId == null || groupChangePhoto.Photo?.Id == null ||
            groupChangePhoto.UserId.Equals(VkApi.UserId))
            return;
        var photo = (ulong) groupChangePhoto.Photo.Id;
        var userId = (long) groupChangePhoto.UserId;

        if (OperationAllowed(group, userId)) return;

        var response = PhraseProcessor.ChangePhoto(group, userId.ToString());

        response += Environment.NewLine +
                    FireEditorWithResponse(group, (long) groupChangePhoto.UserId);

        var photos = ManagerApi.GetPhotos(-group, PhotoAlbumType.Profile);

        if (photos.Any(x => x.Id == (long?) photo))
        {
            ManagerApi.DeletePhoto(photo, -group);
            response += Environment.NewLine + PhraseProcessor.PhotoWasDeleted(group, userId.ToString());
        }

        WatchDog.SendMessage(response);
    }
}
