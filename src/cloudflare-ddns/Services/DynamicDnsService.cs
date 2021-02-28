using CloudflareDDNS.Api;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Serilog;
using System;
using System.Net.Sockets;
using System.Linq;
using CloudflareDDNS.Api.Models;

namespace CloudflareDDNS.Service
{
    public class DynamicDnsService : IDynamicDnsService
    {
        private const string CHECK_TEMPLATE = "managed-by: cloudflare-ddns, check: {0}";
        private readonly ILogger _logger;
        private readonly ICloudflareApi _cloudflare;

        public DynamicDnsService(ILogger logger, ICloudflareApi cloudflare)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (cloudflare is null)
            {
                throw new ArgumentNullException(nameof(cloudflare));
            }

            _logger = logger;
            _cloudflare = cloudflare;
        }

        private static string GetTypeFromIPAddress(IPAddress address) =>
            address.AddressFamily switch
            {
                AddressFamily.InterNetwork => "A",
                AddressFamily.InterNetworkV6 => "AAAA",
                _ => throw new Exception($"Invalid address: can't translate {address} to a DNS type."),
            };

        public async Task UpsertRecords(string zoneId, string subdomain, bool proxied, List<IPAddress> addresses)
        {
            var (_, domain) = await _cloudflare.GetZoneDetails(zoneId);
            var fullDomainName = $"{subdomain}.{domain}";
            var (records, _) = await _cloudflare.ListDNSRecords(zoneId, name: fullDomainName, page: 1, perPage: 100);

            foreach (var ip in addresses)
            {
                var type = GetTypeFromIPAddress(ip);
                var current = records.FirstOrDefault(r => r.Type == type && r.Name == fullDomainName);
                var txts = records.Where(r => r.Type == "TXT");
                var hasCNAME = records.Any(r => r.Type == "CNAME");

                // Bail out when a CNAME record already exists
                if (hasCNAME)
                {
                    _logger.Error("A CNAME record already exists for {domain}, please remove if you intended for cloudflare-ddns to manage this record!", fullDomainName);
                }
                // Record does not exist yet
                else if (current is null)
                {
                    _logger.Verbose("Creating new {type} record for {domain} with address {address}", type, fullDomainName, ip);
                    var record = await _cloudflare.CreateDNSRecord(zoneId, type, fullDomainName, ip.ToString(), 1, proxied);
                    var check = Base64Encode(record.Id);
                    await _cloudflare.CreateDNSRecord(zoneId, "TXT", fullDomainName, string.Format(CHECK_TEMPLATE, check), 1);
                }
                // Record already exists and is managed by cloudflare-ddns
                else if (current is object && HasValidTxtRecord(current, txts))
                {
                    if (current.Content != ip.ToString())
                    {
                        _logger.Information("Updating existing {type} record for {domain} from {previous} to {current}.", type, fullDomainName, current.Content, ip.ToString());
                        await _cloudflare.UpdateDNSRecord(zoneId, current.Id, type, fullDomainName, ip.ToString(), 1, proxied);
                    }
                    else
                    {
                        _logger.Information("Record type {type} for {domain} is already up to date!", type, fullDomainName);
                    }
                }
                else
                {
                    _logger.Information("An {type} record with {domain} already exists that's currently not managed by cloudflare-ddns.", type, domain);
                }
            }
        }

        public static bool HasValidTxtRecord(DnsResult record, IEnumerable<DnsResult> txts)
        {
            var check = string.Format(CHECK_TEMPLATE, Base64Encode(record.Id));
            return txts.FirstOrDefault(t => t.Content == check) != null;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
