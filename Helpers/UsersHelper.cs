using System;
using System.Linq;
using nng_watchdog.Providers;
using nng.DatabaseModels;
using nng.DatabaseProviders;
using nng.Enums;

namespace nng_watchdog.Helpers;

public class UsersHelper
{
    private readonly VkProvider _provider;
    private readonly UsersDatabaseProvider _users;

    public UsersHelper(UsersDatabaseProvider users, VkProvider provider)
    {
        _users = users;
        _provider = provider;
    }

    public bool IsAdmin(long user)
    {
        return _users.Collection.ToList().Any(x => x.UserId.Equals(user) && x.Admin);
    }

    public void BanUser(long user, long group, BanPriority priority)
    {
        if (!TryFindUser(user, out var userObject))
        {
            InsertBannedProfile(user, group, priority);
            return;
        }

        userObject.Banned = true;
        userObject.BannedInfo = new BannedInfo
        {
            GroupId = group,
            Date = DateTime.Now,
            Priority = (int) priority
        };
        userObject.LastUpdated = DateTime.Now;

        _users.Collection.Insert(userObject);
    }

    private void InsertBannedProfile(long user, long group, BanPriority priority)
    {
        var userObject = new User
        {
            UserId = user,
            Name = GetName(user),
            Admin = false,
            Thanks = false,
            App = false,
            Banned = true,
            BannedInfo = new BannedInfo
            {
                Date = DateTime.Now,
                GroupId = group,
                Priority = (int) priority
            },
            LastUpdated = DateTime.Now
        };

        _users.Collection.Insert(userObject);
    }

    private string GetName(long user)
    {
        var userVk = _provider.VkProcessor.VkFramework.GetUser(user);
        return $"{userVk.FirstName} {userVk.LastName}";
    }

    private bool TryFindUser(long user, out User dbUser)
    {
        var users = _users.Collection.ToList();

        dbUser = new User();

        if (!users.Any(x => x.UserId.Equals(user))) return false;

        dbUser = users.First(x => x.UserId.Equals(user));

        return true;
    }
}
