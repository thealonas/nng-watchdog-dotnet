using System;

namespace nng_watchdog.Extensions;

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
