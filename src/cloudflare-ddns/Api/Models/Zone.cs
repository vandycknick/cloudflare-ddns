using System.Text.Json.Serialization;

namespace CloudflareDDNS.Api.Models
{
    public class Zone
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
        public void Deconstruct(out string id, out string name) => (id, name) = (Id, Name);
    }
}
