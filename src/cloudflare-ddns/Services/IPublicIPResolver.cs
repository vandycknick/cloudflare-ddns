using System.Net;
using System.Threading.Tasks;

namespace CloudflareDDNS.Service
{
    public interface IPublicIPResolver
    {
        Task<IPAddress?> ResolveIPv4();
        Task<IPAddress?> ResolveIPv6();
    }
}
