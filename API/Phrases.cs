namespace nng_watchdog.API;

public static class Phrases
{
    public const string BannedAgain = "🚫 @club{GROUP}: @id{BANNED} был повторно заблокирован";
    public const string BannedEditor = "🚫 @club{GROUP}: @id{USER} был заблокирован";
    public const string Block = "🚫 @club{GROUP}: @id{USER} заблокировал @id{BANNED}\nКомментарий: {COM}";
    public const string CannotBanEditor = "🤷‍♂️ @club{GROUP}: @id{USER} не был заблокирован";
    public const string CannotFireEditor = "🤷‍♂️ @club{GROUP}: @id{USER} не был убран из руководителей";
    public const string ChangePhoto = "📷 @club{GROUP}: @id{USER} сменил фотографию";
    public const string FiredEditor = "👮 @club{GROUP}: @id{USER} был убран из руководителей";
    public const string GroupLeave = "⛔ @club{GROUP}: @id{USER} удалён из группы";
    public const string PhotoNew = "📷 @club{GROUP}: @id{USER} добавил новую фотографию";
    public const string PhotoWasDeleted = "📷 @club{GROUP}: фото, загруженное @id{USER}, было удалено";
    public const string Unban = "🙈 @club{GROUP}: @id{USER} разблокировал @id{BANNED}";
    public const string UserDateTime = "@club{GROUP}: @id{USER} был повторно заблокирован по истечению времени";
    public const string WallPost = "📰 @club{GROUP}: @id{USER} опубликовал новую запись";
    public const string WallPostTemp = "📰 @club{GROUP}: кто-то опубликовал новую запись";
}
