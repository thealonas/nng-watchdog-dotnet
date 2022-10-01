using nng_watchdog.Extensions;

namespace nng_watchdog.API;

public static class PhraseProcessor
{
    public static string UserBlock(long group, string user, string banned, string com)
    {
        return Phrases.Block
            .SetGroup(group.ToString()).SetUser(user)
            .SetBanned(banned).SetCom(com);
    }

    public static string UserUnblock(long group, string user, string banned)
    {
        return Phrases.Unban
            .SetGroup(group.ToString())
            .SetUser(user)
            .SetBanned(banned);
    }

    public static string PhotoNew(long group, string user)
    {
        return Phrases.PhotoNew
            .SetGroup(group.ToString())
            .SetUser(user);
    }

    public static string ChangePhoto(long group, string user)
    {
        return Phrases.ChangePhoto
            .SetGroup(group.ToString())
            .SetUser(user);
    }

    public static string PhotoWasDeleted(long group, string userId)
    {
        return Phrases.PhotoWasDeleted
            .SetGroup(group.ToString())
            .SetUser(userId);
    }

    public static string WallPost(long group, string user)
    {
        return Phrases.WallPost
            .SetGroup(group.ToString())
            .SetUser(user);
    }

    public static string WallPostTemp(long group)
    {
        return Phrases.WallPostTemp
            .SetGroup(group.ToString());
    }

    public static string FiredEditor(long group, string user, bool state)
    {
        return (state ? Phrases.FiredEditor : Phrases.CannotFireEditor)
            .SetUser(user)
            .SetGroup(group.ToString());
    }

    public static string GroupLeave(long group, string user)
    {
        return Phrases.GroupLeave
            .SetUser(user)
            .SetGroup(group.ToString());
    }

    public static string BannedAgain(long group, string user)
    {
        return Phrases.BannedAgain
            .SetGroup(group.ToString())
            .SetBanned(user);
    }

    public static string BannedEditor(long group, string user, bool state)
    {
        return (state ? Phrases.BannedEditor : Phrases.CannotBanEditor)
            .SetGroup(group.ToString())
            .SetUser(user);
    }

    public static string UserDateTime(long group, string user)
    {
        return Phrases.UserDateTime
            .SetGroup(group.ToString())
            .SetUser(user);
    }
}
