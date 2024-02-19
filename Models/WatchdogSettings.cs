using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Redis.OM.Modeling;

namespace nng_watchdog.Models;

[Document(IndexName = "watchdog-settings", StorageType = StorageType.Json, Prefixes = new[] {"settings:watchdog"})]
public class WatchdogSettings
{
    public WatchdogSettings(string groupToken)
    {
        GroupToken = groupToken;
    }

    [JsonProperty("group_token")]
    [JsonPropertyName("group_token")]
    [Indexed(PropertyName = "group_token")]
    public string GroupToken { get; set; }
}
