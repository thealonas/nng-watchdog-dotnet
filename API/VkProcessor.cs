using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using nng.Extensions;
using nng.VkFrameworks;
using VkNet.Model.GroupUpdate;
using VkNet.Utils;

namespace nng_watchdog.API;

public class VkProcessor
{
    private readonly HttpClient _httpClient;
    public readonly VkFramework VkFramework;

    public VkProcessor(VkFramework vkFramework)
    {
        VkFramework = vkFramework;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true
        };

        _httpClient.BaseAddress = new Uri("https://api.vk.com/method/");
    }

    public bool HasPermission(long user, long group)
    {
        return VkFrameworkExecution.ExecuteWithReturn(() =>
            bool.Parse(VkFramework.Api.Call("execute.watchdogHasPermission", new VkParameters
            {
                {"user", user},
                {"group", group}
            })["found"]));
    }

    public void BanEditor(long editor, long group, string comment)
    {
        BaseCall("execute.watchdogBanEditor", new VkParameters
        {
            {"group", group},
            {"user", editor},
            {"banComment", comment}
        });
    }

    public void ProcessChangePhoto(long group)
    {
        BaseCall("execute.watchdogProcessChangePhoto", new VkParameters
        {
            {"group", group}
        });
    }

    public void ProcessUnblock(UserUnblock @event, long group, string comment)
    {
        BaseCall("execute.watchdogProcessUnblock", new VkParameters
        {
            {"group", group},
            {"admin", @event.AdminId},
            {"user", @event.UserId},
            {"banComment", comment}
        });
    }

    public void ProcessPost(long group, long post)
    {
        BaseCall("execute.watchdogProcessPost", new VkParameters
        {
            {"group", group},
            {"post", post}
        });
    }

    public void ProcessPhoto(long group, long photo)
    {
        BaseCall("execute.watchdogProcessPhoto", new VkParameters
        {
            {"group", group},
            {"photo", photo}
        });
    }

    public Dictionary<long, string> GetSecrets(IEnumerable<long> groups)
    {
        var groupsByTwenty = groups.TakeBy(20);
        var data = new Dictionary<long, string>();
        foreach (var answer in groupsByTwenty.Select(GetSecretsPartially))
        foreach (var (group, secret) in answer)
            data[group] = secret;

        return data;
    }

    private Dictionary<long, string> GetSecretsPartially(IReadOnlyCollection<long> groupsByTwenty)
    {
        var result = VkFrameworkExecution.ExecuteWithReturn(() =>
        {
            var vkResponse = VkFramework.Api
                .Call("execute.watchdogGetSecrets", new VkParameters
                {
                    {"groups", string.Join(",", groupsByTwenty)},
                    {"serverName", "watchdog"}
                }).ToString();

            return JsonConvert.DeserializeObject<IReadOnlyCollection<Dictionary<string, object>>>(vkResponse);
        });

        if (result is null) throw new NullReferenceException();

        var output = new Dictionary<long, string>();

        foreach (var dict in result)
        {
            var entry = SearchForGroupAndSecret(dict);
            output[entry.Key] = entry.Value;
        }

        return output;
    }

    private KeyValuePair<long, string> SearchForGroupAndSecret(IReadOnlyDictionary<string, object> dictionary)
    {
        var output = new KeyValuePair<long, string>(
            long.Parse(dictionary["group"]
                .ToString() ?? throw new ArgumentNullException(null, nameof(dictionary))),
            dictionary["secret"].ToString() ?? throw new ArgumentException(null, nameof(dictionary)));
        return output;
    }

    private void BaseCall(string methodName, VkParameters parameters)
    {
        VkFrameworkExecution.Execute(() => { VkFramework.Api.Call(methodName, parameters); });
    }
}
