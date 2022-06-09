using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace nng_watchdog.Models;

public class Event
{
    public Event(string type, long groupId, JObject? o, string secret)
    {
        Type = type;
        GroupId = groupId;
        Object = o;
        Secret = secret;
    }

    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("group_id")] public long GroupId { get; set; }

    [JsonProperty("object")] public JObject? Object { get; set; }

    [JsonProperty("secret")] public string Secret { get; set; }
}
