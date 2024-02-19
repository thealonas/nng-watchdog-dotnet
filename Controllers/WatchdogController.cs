using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using nng_watchdog.API;
using nng_watchdog.Helpers;
using nng_watchdog.Providers;
using nng.DatabaseModels;
using nng.DatabaseProviders;
using nng.Enums;
using nng.Extensions;
using Sentry;
using VkNet.Model.Attachments;
using VkNet.Model.GroupUpdate;
using VkNet.Utils;
using Event = nng_watchdog.Models.Event;

namespace nng_watchdog.Controllers;

[ApiController]
[Route("")]
public class WatchdogController : ControllerBase
{
    private readonly GroupSecretsProvider _groupSecrets;
    private readonly ILogger<WatchdogController> _logger;
    private readonly PhotoHelper _photoHelper;
    private readonly Settings _settings;
    private readonly UsersHelper _usersHelper;

    public WatchdogController(VkProvider provider, WatchDogApi watchDogApi,
        ILogger<WatchdogController> logger, SettingsDatabaseProvider settings,
        GroupSecretsProvider groupSecrets, PhotoHelper photoHelper, UsersHelper usersHelper)
    {
        _logger = logger;
        _groupSecrets = groupSecrets;
        _photoHelper = photoHelper;
        _usersHelper = usersHelper;

        ManagerApi = provider.VkProcessor;
        WatchDog = watchDogApi;

        if (!settings.Collection.TryGetById("main", out var mainSettings))
            throw new ArgumentException(null, nameof(settings));

        _settings = mainSettings;
    }

    private WatchDogApi WatchDog { get; }

    private VkProcessor ManagerApi { get; }

    [HttpPost]
    public IActionResult Callback([FromBody] Event vkEvent)
    {
        _logger.LogInformation("Получили ивент от VK");
        _logger.LogInformation("Сообщество: {Group}\n\tТип: {Type}",
            vkEvent.GroupId, vkEvent.Type);

        string secret;
        string confirm;
        try
        {
            var targetGroup = _groupSecrets.Collection.ToList().First(x => x.GroupId.Equals(vkEvent.GroupId));
            secret = targetGroup.Secret;
            confirm = targetGroup.Confirm;
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

            try
            {
                _logger.LogInformation("Пришел запрос на подтверждение сервера в сообществе {GroupId}" +
                                       "\nВозвращаем {Token}", group, confirm);
                return Ok(confirm);
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                _logger.LogError("Не удалось получить конфирм для группы {GroupId}", group);
                return Ok("ok");
            }
        }

        Task.Run(() => { ProcessEvent(vkEvent); }).ContinueWith(task =>
        {
            if (task is {IsFaulted: true, Exception: { }})
                SentrySdk.CaptureException(task.Exception);
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
                ProcessWall(vkEvent);
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
        if (photo.UserId == null || photo.UserId == ManagerApi.VkFramework.CurrentUser.Id) return;
        var admin = (long) photo.UserId;
        if (ManagerApi.HasPermission(admin, group) || _usersHelper.IsAdmin(admin)) return;

        ManagerApi.ProcessPhoto(group, admin);
        _usersHelper.BanUser(admin, group, BanPriority.Orange);

        var response = PhraseProcessor.PhotoNew(group, admin.ToString() ?? throw new NullReferenceException());

        WatchDog.SendMessage(response);
    }

    private void ProcessWall(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var post = Post.FromJson(new VkResponse(vkEvent.Object));
        if (post.CreatedBy == null || post.Id == null ||
            post.CreatedBy == ManagerApi.VkFramework.CurrentUser.Id) return;
        var admin = (long) post.CreatedBy;
        if (ManagerApi.HasPermission(admin, group) || _usersHelper.IsAdmin(admin)) return;
        var response = PhraseProcessor.WallPost(group, admin.ToString());

        ManagerApi.ProcessPost(group, (long) post.Id);

        ManagerApi.BanEditor(admin, group, _settings.BanComment);

        _usersHelper.BanUser(admin, group, BanPriority.Green);

        response += $"\n{PhraseProcessor.BannedEditor(group, admin.ToString(), true)}";

        WatchDog.SendMessage(response);
    }

    private void ProcessUserBlock(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var block = UserBlock.FromJson(new VkResponse(vkEvent.Object));
        if (block.UserId is null || block.AdminId is null ||
            block.AdminId.Equals(ManagerApi.VkFramework.CurrentUser.Id)) return;
        var admin = (long) block.AdminId;
        var banned = (long) block.UserId;
        if (ManagerApi.HasPermission(admin, group) || _usersHelper.IsAdmin(admin)) return;

        var response = PhraseProcessor.UserBlock(group, admin.ToString(), banned.ToString(), block.Comment);
        response += $"\n{PhraseProcessor.BannedEditor(group, admin.ToString(), true)}";
        response += $"\n{PhraseProcessor.BannedEditor(group, banned.ToString(), true)}";

        ManagerApi.BanEditor(admin, group, _settings.BanComment);
        ManagerApi.BanEditor(banned, group, _settings.BanComment);

        _usersHelper.BanUser(admin, group, BanPriority.Red);

        WatchDog.SendMessage(response);
    }

    private void ProcessUserUnBlock(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var unblock = UserUnblock.FromJson(new VkResponse(vkEvent.Object));

        if (unblock.UserId == null || unblock.AdminId == null ||
            Equals(unblock.AdminId, ManagerApi.VkFramework.CurrentUser.Id)) return;

        var admin = (long) unblock.AdminId;
        var user = (long) unblock.UserId;

        if (ManagerApi.HasPermission(admin, group) || _usersHelper.IsAdmin(admin)) return;

        var response = PhraseProcessor.UserUnblock(group, admin.ToString(), user.ToString());

        ManagerApi.ProcessUnblock(unblock, group, _settings.BanComment);

        _usersHelper.BanUser(admin, group, BanPriority.Red);

        response +=
            $"\n{PhraseProcessor.BannedEditor(group, admin.ToString(), true)}" +
            $"\n{PhraseProcessor.BannedAgain(group, user.ToString())}";

        WatchDog.SendMessage(response);
    }

    private void ProcessGroupLeave(Event vkEvent)
    {
        var leave = GroupLeave.FromJson(new VkResponse(vkEvent.Object));

        if (leave.UserId == null || leave.IsSelf == null || (bool) leave.IsSelf ||
            leave.UserId == ManagerApi.VkFramework.CurrentUser.Id) return;

        var response = PhraseProcessor.GroupLeave(vkEvent.GroupId, leave.UserId.ToString()!);

        WatchDog.SendMessage(response);
    }

    private void ProcessChangePhoto(Event vkEvent)
    {
        var group = vkEvent.GroupId;
        var groupChangePhoto = GroupChangePhoto.FromJson(new VkResponse(vkEvent.Object));
        if (groupChangePhoto.UserId == null || groupChangePhoto.UserId.Equals(ManagerApi.VkFramework.CurrentUser.Id))
            return;
        var userId = (long) groupChangePhoto.UserId;
        if (ManagerApi.HasPermission(userId, group) || _usersHelper.IsAdmin(userId)) return;

        var response = PhraseProcessor.ChangePhoto(group, userId.ToString());

        ManagerApi.BanEditor(userId, group, _settings.BanComment);
        ManagerApi.ProcessChangePhoto(group);
        _photoHelper.SetAvatar(group);
        _usersHelper.BanUser(userId, group, BanPriority.Orange);

        response += $"\n{PhraseProcessor.BannedEditor(group, userId.ToString(), true)}" +
                    $"\n{PhraseProcessor.PhotoWasDeleted(group, userId.ToString())}" +
                    $"\n{PhraseProcessor.PhotoUploaded(group)}";

        WatchDog.SendMessage(response);
    }
}
