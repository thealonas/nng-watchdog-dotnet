namespace nng_watchdog.API;

public static class Phrases
{
    public const string BannedAgain = "🚫 @club{GROUP}: @id{BANNED} был повторно заблокирован";
    public const string BannedEditor = "🚫 @club{GROUP}: @id{USER} был заблокирован";
    public const string Block = "🚫 @club{GROUP}: @id{USER} заблокировал @id{BANNED}\nКомментарий: {COM}";
    public const string CannotBanEditor = "🤷‍♂️ @club{GROUP}: @id{USER} не был заблокирован";
    public const string ChangePhoto = "📷 @club{GROUP}: @id{USER} сменил фотографию";
    public const string GroupLeave = "⛔ @club{GROUP}: @id{USER} удалён из группы";
    public const string PhotoUploaded = "📷 @club{GROUP}: новая аватарка была загружена";
    public const string PhotoNew = "📷 @club{GROUP}: @id{USER} добавил новую фотографию";
    public const string PhotoWasDeleted = "📷 @club{GROUP}: фото, загруженное @id{USER}, было удалено";
    public const string Unban = "🙈 @club{GROUP}: @id{USER} разблокировал @id{BANNED}";
    public const string WallPost = "📰 @club{GROUP}: @id{USER} опубликовал новую запись";
}
