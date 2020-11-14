using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CloudflareDDNS.Service
{
    public class PublicIPResolver : IPublicIPResolver
    {
        private readonly IEnumerable<IPublicIPResolver> _resolvers;

        public PublicIPResolver(params IPublicIPResolver[] resolvers)
        {
            if (resolvers is null)
            {
                throw new System.ArgumentNullException(nameof(resolvers));
            }

            _resolvers = resolvers;
        }
        public async Task<IPAddress?> ResolveIPv4()
        {
            foreach (var resolver in _resolvers)
            {
                var ip = await resolver.ResolveIPv4();

                if (ip is object)
                {
                    return ip;
                }
            }

            return null;
        }

        public async Task<IPAddress?> ResolveIPv6()
        {
            foreach (var resolver in _resolvers)
            {
                var ip = await resolver.ResolveIPv6();

                if (ip is object)
                {
                    return ip;
                }
            }

            return null;
        }
    }
}
