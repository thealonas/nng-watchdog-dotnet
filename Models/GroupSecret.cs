using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Redis.OM.Modeling;

namespace nng_watchdog.Models;

[Document(IndexName = "group-secrets", StorageType = StorageType.Json, Prefixes = new[] {"settings:secrets:watchdog"})]
public class GroupSecret
{
    public GroupSecret(long groupId, string secret, string confirm)
    {
        GroupId = groupId;
        Secret = secret;
        Confirm = confirm;
    }

    [RedisIdField]
    [JsonProperty("group_id")]
    [JsonPropertyName("group_id")]
    [Indexed(PropertyName = "group_id")]
    public long GroupId { get; set; }

    [JsonProperty("group_token")]
    [JsonPropertyName("group_token")]
    [Indexed(PropertyName = "group_token")]
    public string Secret { get; set; }

    [JsonProperty("group_confirm")]
    [JsonPropertyName("group_confirm")]
    [Indexed(PropertyName = "group_confirm")]
    public string Confirm { get; set; }
}
