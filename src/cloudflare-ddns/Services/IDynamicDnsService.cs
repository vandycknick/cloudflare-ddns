using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CloudflareDDNS.Service
{
    public interface IDynamicDnsService
    {
        Task UpsertRecords(string zoneId, string subdomain, bool proxied, List<IPAddress> addresses);
    }
}
