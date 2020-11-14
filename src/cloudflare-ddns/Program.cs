using CloudflareDDNS.Api;
using CloudflareDDNS.Service;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;
using Mono.Options;
using System.Reflection;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using DnsClient;

namespace CloudflareDDNS
{
    public static class Program
    {
        static async Task<int> Main(string[] args)
        {
            var configFile = "config.json";
            var showHelp = false;
            var showVersion = false;
            var logLevel = "info";
            var options = new OptionSet
            {
                { "c|config", "Path to the config.json file, defaults to the current working directory.", (string c) => configFile = c },
                { "l|log-level=", "Set the log level (verbose, info, warning, error), defaults to info", (string l) => logLevel = l ?? logLevel},
                { "v|version", "Show current version.", v => showVersion = v != null },
                { "?|h|help", "Show help information", h => showHelp = h != null },
            };

            try
            {
                options.Parse(args);

                if (showHelp)
                {
                    Console.WriteLine($"{GetName()}: {GetVersion()}");
                    Console.WriteLine();
                    Console.WriteLine("Cloudflare DDNS");
                    Console.WriteLine();
                    Console.WriteLine($"Usage: {GetName()} [options]");
                    Console.WriteLine();
                    Console.WriteLine("Options");
                    Console.WriteLine();
                    options.WriteOptionDescriptions(Console.Out);
                    return 0;
                }
                else if (showVersion)
                {
                    Console.WriteLine(GetVersion());
                    return 0;
                }

                await ExecuteAsync(configFile, logLevel);
                return 0;
            }
            catch (OptionException ex)
            {
                Console.WriteLine($"{GetName()}: {GetVersion()}");
                Console.WriteLine(ex.Message);
                Console.WriteLine($"Try `{GetName()} --help' for more information.");
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{GetName()}: {GetVersion()}");
                Console.WriteLine();
                Console.WriteLine("Oh no something went wrong 😱:");
                Console.WriteLine(ex.Message);
                return 1;
            }
        }

        static async Task ExecuteAsync(string configFile, string logLevel)
        {
            var config = await Config.LoadFromAsync(configFile);

            using var client = new HttpClient();
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Is(Utils.MapToLogEventLevel(logLevel))
                .CreateLogger();

            var resolvers = new Dictionary<string, IPublicIPResolver>
            {
                {
                    "http",
                    new PublicIPResolverOverHttp(client, logger, new PublicIPResolverOverHttpOptions(
                        ipv4Endpoint: config.Resolvers.Http.IPv4Endpoint,
                        ipv6Endpoint: config.Resolvers.Http.IPv6Endpoint
                    ))
                },
                {
                    "dns",
                    new PublicIPResolverOverDns(
                        client: new LookupClient(),
                        logger: logger,
                        options: PublicIPResolverOverDns.GetOptionsForDnsServer(config.Resolvers.DnsServer)
                    )
                }
            };

            var resolver = new PublicIPResolver(config.Resolvers.Order.Select(r => resolvers[r]).ToArray());
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(config.ApiToken));
            var ddns = new DynamicDnsService(logger, cloudflare);

            await SyncDNSCommand(logger, resolver, ddns, config);
        }

        public static async Task SyncDNSCommand(ILogger logger, IPublicIPResolver resolver, IDynamicDnsService ddns, Config appConfig)
        {
            var addresses = new List<IPAddress>();

            if (appConfig.IPv4)
            {
                var ip = await resolver.ResolveIPv4();
                if (ip is object) addresses.Add(ip);
            }

            if (appConfig.IPv6)
            {
                var ip = await resolver.ResolveIPv6();
                if (ip is object) addresses.Add(ip);
            }

            if (addresses.Count == 0)
            {
                logger.Warning("Couldn't resolve your public IPv4 or IPv6 address, check your configuration and connectivity. Skipping ddns sync!");
                return;
            }

            foreach (var item in appConfig.Records)
            {
                await ddns.UpsertRecords(item.ZoneId, item.Subdomain, item.Proxied, addresses);
            }
        }

        private static string GetVersion()
            => typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

        private static string GetName()
            => typeof(Program).Assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "";
    }
}
