namespace CloudflareDDNS.Service
{
    public class PublicIPResolverOverHttpOptions
    {
        public PublicIPResolverOverHttpOptions(string ipv4Endpoint, string ipv6Endpoint)
        {
            IPv4Endpoint = ipv4Endpoint;
            IPv6Endpoint = ipv6Endpoint;
        }

        public string IPv4Endpoint { get; set; }
        public string IPv6Endpoint { get; set; }
    }
}
