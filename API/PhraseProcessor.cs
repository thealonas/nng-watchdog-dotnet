using System;
using Microsoft.Extensions.Configuration;

namespace nng_watchdog.API;

public static class StringExtensions
{
    private static string SetBase(this string str, string id, object val)
    {
        return str.Replace(id, val.ToString(), StringComparison.CurrentCulture);
    }

    public static string SetGroup(this string str, object val)
    {
        return str.SetBase("{GROUP}", val);
    }

    public static string SetUser(this string str, object val)
    {
        return str.SetBase("{USER}", val);
    }

    public static string SetBanned(this string str, object val)
    {
        return str.SetBase("{BANNED}", val);
    }

    public static string SetCom(this string str, object val)
    {
        return str.SetBase("{COM}", val);
    }
}

public class PhraseProcessor
{
    public PhraseProcessor(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    private string GetPhrase(string path)
    {
        return Configuration[$"Phrases:{path}"];
    }

    public string UserBlock(long group, string user, string banned, string com)
    {
        return GetPhrase("Block")
            .SetGroup(group.ToString()).SetUser(user)
            .SetBanned(banned).SetCom(com);
    }

    public string UserUnblock(long group, string user, string banned)
    {
        return GetPhrase("Unban")
            .SetGroup(group.ToString())
            .SetUser(user)
            .SetBanned(banned);
    }

    public string PhotoNew(long group, string user)
    {
        return GetPhrase("PhotoNew")
            .SetGroup(group.ToString())
            .SetUser(user);
    }

    public string ChangePhoto(long group, string user)
    {
        return GetPhrase("ChangePhoto")
            .SetGroup(group.ToString())
            .SetUser(user);
    }

    public string PhotoWasDeleted(long group, string userId)
    {
        return GetPhrase("PhotoWasDeleted")
            .SetGroup(group.ToString())
            .SetUser(userId);
    }

    public string WallPost(long group, string user)
    {
        return GetPhrase("WallPost")
            .SetGroup(group.ToString())
            .SetUser(user);
    }

    public string FiredEditor(long group, string user, bool state)
    {
        return GetPhrase(state ? "FiredEditor" : "CannotFireEditor")
            .SetUser(user)
            .SetGroup(group.ToString());
    }

    public string GroupLeave(long group, string user)
    {
        return GetPhrase("GroupLeave")
            .SetUser(user)
            .SetGroup(group.ToString());
    }

    public string BannedAgain(long group, string user)
    {
        return GetPhrase("BannedAgain")
            .SetGroup(group.ToString())
            .SetBanned(user);
    }

    public string BannedEditor(long group, string user, bool state)
    {
        return GetPhrase(state ? "BannedEditor" : "CannotBanEditor")
            .SetGroup(group.ToString())
            .SetUser(user);
    }

    public string UserDateTime(long group, string user)
    {
        return GetPhrase("UserDateTime")
            .SetGroup(group.ToString())
            .SetUser(user);
    }
}
