using CloudflareDDNS.Api;
using CloudflareDDNS.Service;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;
using Serilog.Formatting.Compact;
using Mono.Options;
using System.Reflection;
using System;
using System.IO.Abstractions;
using Serilog.Events;

namespace CloudflareDDNS
{
    public class Program
    {
        static async Task<int> Main(string[] args)
        {
            var configFile = "config.json";
            var showHelp = false;
            var showVersion = false;
            var verbose = false;
            var options = new OptionSet
            {
                { "c|config", "Path to the config.json file, defaults to the current working directory.", (string c) => configFile = c},
                { "v|version", "Show current version.", v => showVersion = v != null },
                { "?|h|help", "Show help information", h => showHelp = h != null },
            };

            try
            {
                var extra = options.Parse(args);

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

                await Execute(configFile, verbose);
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

        static async Task Execute(string configFile, bool verbose)
        {
            var appConfig = await Config.LoadFromAsync(configFile);

            using var client = new HttpClient();
            var fileSystem = new FileSystem();
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Is(verbose ? LogEventLevel.Verbose : LogEventLevel.Information)
                .CreateLogger();

            var ipResolver = new IPResolverService(client, logger, new IPResolverServiceOptions(appConfig.IPv4Resolver, appConfig.IPv6Resolver));
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(appConfig.ApiKey));
            var ddns = new DynamicDnsService(logger, cloudflare);

            await SyncDDNSCommand(logger, ipResolver, ddns, appConfig);
        }

        public static async Task SyncDDNSCommand(ILogger logger, IPResolverService ipResolver, DynamicDnsService ddns, Config appConfig)
        {
            var ips = await ipResolver.Resolve();
            foreach (var item in appConfig.Dns)
            {
                await ddns.UpsertRecords(item.ZoneId, item.Domain, item.Proxied, ips);
            }
        }

        private static string GetVersion()
            => typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

        private static string GetName()
            => typeof(Program).Assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "";
    }
}
