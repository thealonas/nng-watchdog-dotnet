using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using nng_watchdog.API;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
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
        IConfiguration configuration, ILogger<WatchdogController> logger, PhraseProcessor phrases)
    {
        _logger = logger;

        Configuration = configuration;
        VkApi = vkApi;
        ManagerApi = managerApi;
        WatchDog = watchDogApi;
        Phrases = phrases;
    }

    private IConfiguration Configuration { get; }

    private IVkApi VkApi { get; }

    private WatchDogApi WatchDog { get; }

    private PhraseProcessor Phrases { get; }

    private VkProcessor ManagerApi { get; }

    private async Task<bool> OperationAllowedAsync(long group, long user)
    {
        var perms = await ManagerApi.GetUserPermissionsAsync(group, user);
        _logger.LogDebug("Права пользователя {User} в сообществе {Group} — {Perms}",
            user, group, perms?.ToString());
        return perms == ManagerRole.Administrator || perms == ManagerRole.Creator;
    }

    private string FireEditorWithResponse(long group, long user)
    {
        var fireState = ManagerApi.FireEditor(group, user);
        var banState = ManagerApi.Block(group, user, Configuration["Data:BanComment"]);

        return Phrases.FiredEditor(group, user.ToString(), fireState)
               + Environment.NewLine
               + Phrases.BannedEditor(group, user.ToString(), banState);
    }

    [HttpPost]
    public async Task<IActionResult> CallbackAsync([FromBody] Event vkEvent)
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
            _logger.LogWarning("У запроса с типом {Type} неправильный secret ({ActualSecret})", vkEvent.Type,
                vkEvent.Secret);
            return Ok("ok");
        }

        var group = vkEvent.GroupId;
        switch (vkEvent.Type)
        {
            case "confirmation":
                var token = Configuration.GetSection($"{group}:Confirm").Value;
                _logger.LogInformation("Пришел запрос на подтверждение сервера в сообществе {GroupId}" +
                                       "\nВозвращаем {Token}", group, token);
                return Ok(token);

            case "photo_new":
                await ProcessPhotoAsync(vkEvent);
                break;

            case "wall_repost":
            case "wall_post_new":
                await ProcessWallAsync(vkEvent);
                break;

            case "user_block":
                await ProcessUserBlockAsync(vkEvent);
                break;

            case "user_unblock":
                await ProcessUserUnBlockAsync(vkEvent);
                break;

            case "group_leave":
                ProcessGroupLeave(vkEvent);
                break;

            case "group_change_photo":
                await ProcessChangePhotoAsync(vkEvent);
                break;

            default:
                _logger.LogWarning("Неизвестный тип запроса");
                break;
        }

        return Ok("ok");
    }

    private async Task ProcessPhotoAsync(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var photo = Photo.FromJson(new VkResponse(vkEvent.Object));
        if (photo.UserId == null || photo.UserId == VkApi.UserId) return;
        if (await OperationAllowedAsync(group, (long) photo.UserId)) return;

        var response = Phrases.PhotoNew(group, photo.OwnerId.ToString() ?? throw new NullReferenceException());

        WatchDog.SendMessage(response);
    }

    private async Task ProcessWallAsync(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var post = Post.FromJson(new VkResponse(vkEvent.Object));
        if (post.CreatedBy == null || post.Id == null || post.CreatedBy == VkApi.UserId) return;
        if (await OperationAllowedAsync(group, (long) post.CreatedBy)) return;
        var response = Phrases.WallPost(group, post.CreatedBy.ToString() ?? throw new NullReferenceException());

        ManagerApi.WallProcessor(group, true);
        await Task.Delay(1000);
        ManagerApi.DeletePost(group, post.Id);
        await Task.Delay(1000);
        ManagerApi.WallProcessor(group, false);

        response += Environment.NewLine;
        response += FireEditorWithResponse(group, (long) post.CreatedBy);

        WatchDog.SendMessage(response);
    }

    private async Task ProcessUserBlockAsync(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var block = UserBlock.FromJson(new VkResponse(vkEvent.Object));
        if (block.UserId == null || block.AdminId == null || block.AdminId == VkApi.UserId) return;
        if (await OperationAllowedAsync(group, (long) block.AdminId)) return;

        var response = Phrases.UserBlock(group, block.AdminId.ToString()!, block.UserId.ToString()!,
            block.Comment);

        response += Environment.NewLine;
        response += FireEditorWithResponse(group, (long) block.AdminId);

        WatchDog.SendMessage(response);
    }

    private async Task ProcessUserUnBlockAsync(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var unblock = UserUnblock.FromJson(new VkResponse(vkEvent.Object));
        if (unblock.UserId == null || unblock.AdminId == null ||
            Equals(unblock.AdminId, VkApi.UserId)) return;
        if (await OperationAllowedAsync(group, (long) unblock.AdminId)) return;
        if (unblock.ByEndDate is true)
        {
            var user = (long) unblock.UserId;
            var dateTimeResponse = Phrases.UserDateTime(group, user.ToString());
            ManagerApi.Block(group, user, Configuration["Data:BanComment"]);
            dateTimeResponse += Phrases.BannedAgain(group, user.ToString());
            WatchDog.SendMessage(dateTimeResponse);
            return;
        }

        var response = Phrases.UserUnblock(group, unblock.AdminId.ToString()!, unblock.UserId.ToString()!);

        response += Environment.NewLine +
                    FireEditorWithResponse(group, (long) unblock.AdminId);

        ManagerApi.Block(group, (long) unblock.UserId, Configuration["Data:BanComment"]);

        response += Environment.NewLine + Phrases.BannedAgain(group, unblock.UserId.ToString()!);

        WatchDog.SendMessage(response);
    }

    private void ProcessGroupLeave(Event vkEvent)
    {
        var leave = GroupLeave.FromJson(new VkResponse(vkEvent.Object));
        if (leave.UserId == null || leave.IsSelf == null || (bool) leave.IsSelf || leave.UserId == VkApi.UserId)
            return;
        var response = Phrases.GroupLeave(vkEvent.GroupId, leave.UserId.ToString()!);
        WatchDog.SendMessage(response);
    }

    private async Task ProcessChangePhotoAsync(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var groupChangePhoto = GroupChangePhoto.FromJson(new VkResponse(vkEvent.Object));
        if (groupChangePhoto.UserId == null || groupChangePhoto.Photo?.Id == null ||
            groupChangePhoto.UserId.Equals(VkApi.UserId))
            return;

        var photo = (ulong) groupChangePhoto.Photo.Id;
        var userId = (long) groupChangePhoto.UserId;

        if (await OperationAllowedAsync(group, userId)) return;

        var response = Phrases.ChangePhoto(group, userId.ToString());

        response += Environment.NewLine +
                    FireEditorWithResponse(group, (long) groupChangePhoto.UserId);

        ManagerApi.DeletePhoto(photo, (ulong) -vkEvent.GroupId);

        response += Environment.NewLine + Phrases.PhotoWasDeleted(group, userId.ToString());

        WatchDog.SendMessage(response);
    }
}
