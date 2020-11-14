using System.Collections.Generic;
using System.Net;

namespace CloudflareDDNS.Service
{
    public class PublicIPResolverOverDnsOptions : PublicIPResolverOptions
    {
        public PublicIPResolverOverDnsOptions(
            string query,
            bool isTxt,
            List<IPAddress> ipv4NameServers,
            List<IPAddress> ipv6NameServers
        )
        {
            Query = query;
            IsTXT = isTxt;
            IPv4NameServers = ipv4NameServers;
            IPv6NameServers = ipv6NameServers;
        }

        public string Query { get; set; }
        public bool IsTXT { get; set; }
        public List<IPAddress> IPv4NameServers { get; set; }
        public List<IPAddress> IPv6NameServers { get; set; }
    }
}
