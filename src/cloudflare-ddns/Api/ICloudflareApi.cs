using CloudflareDDNS.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudflareDDNS.Api
{
    public interface ICloudflareApi
    {
        Task<Zone> GetZoneDetails(string zoneId);

        Task<(List<DNSResult> results, ApiResultPager? pager)> ListDNSRecords(
            string zoneId, string? type = null, string? name = null, int page = 1, int perPage = 20
        );

        Task<DNSResult> CreateDNSRecord(string zoneId, string type, string name, string content, long? ttl = null, bool? proxied = null);

        Task<DNSResult> UpdateDNSRecord(string zoneId, string id, string type, string name, string content, long? ttl = null, bool? proxied = null);
    }
}
