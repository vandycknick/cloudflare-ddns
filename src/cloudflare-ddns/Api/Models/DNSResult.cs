using System;
using System.Text.Json.Serialization;

namespace CloudflareDDNS.Api.Models
{
    public class DnsResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("proxiable")]
        public bool? Proxiable { get; set; }

        [JsonPropertyName("proxied")]
        public bool? Proxied { get; set; }

        [JsonPropertyName("ttl")]
        public long? Ttl { get; set; }

        [JsonPropertyName("locked")]
        public bool? Locked { get; set; }

        [JsonPropertyName("zone_id")]
        public string ZoneId { get; set; } = "";

        [JsonPropertyName("zone_name")]
        public string ZoneName { get; set; } = "";

        [JsonPropertyName("created_on")]
        public DateTimeOffset? CreatedOn { get; set; }

        [JsonPropertyName("modified_on")]
        public DateTimeOffset? ModifiedOn { get; set; }
    }

}
