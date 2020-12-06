using System.Collections.Generic;
using System.Net;
using DnsClient;

namespace CloudflareDDNS.Service
{
    public class PublicIPResolverOverDnsOptions : PublicIPResolverOptions
    {
        public PublicIPResolverOverDnsOptions(
            string query,
            QueryType type,
            QueryClass cls,
            List<IPAddress> ipv4NameServers,
            List<IPAddress> ipv6NameServers
        )
        {
            Query = query;
            QueryType = type;
            QueryClass = cls;
            IPv4NameServers = ipv4NameServers;
            IPv6NameServers = ipv6NameServers;
        }

        public string Query { get; set; }
        public QueryType QueryType { get; set; }
        public QueryClass QueryClass { get; set; }
        public List<IPAddress> IPv4NameServers { get; set; }
        public List<IPAddress> IPv6NameServers { get; set; }
    }
}
