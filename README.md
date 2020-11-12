# Cloudflare DDNS
[![Build status][ci-badge]][ci-url]
[![NuGet][nuget-package-badge]][nuget-package-url]
[![feedz.io][feedz-package-badge]][feedz-package-url]
[![Coverage][sonar-cloud-coverage-badge]][sonar-cloud-coverage-url]

> Dynamic DNS service based on Cloudflare! Access your home network remotely via a custom domain name without a static IP!

## What it does
Cloudflare DDNS allows you to run your own Dynamic DNS service from the comfort of your own infrastructure. Combined with a Raspberry PI or whatever spare hardware you still have lying around, it helps promotes a more decentralized internet at a low cost. During execution, it resolves your current public IPv4 and IPv6 addresses and creates/updates DNS records in Cloudflare to point to your home address. It keeps track of any DNS records that it is managing, giving you some peace of mind and allowing it to run in non-empty Cloudflare zones without having to worry that your public blog gets taken down. Any stale or duplicate records will get safely cleaned up.

## Getting started

### Local Installation
For a local installation make sure you have the latest [.NET Core SDK](https://dotnet.microsoft.com/download) installed for your platform.

Once installed, run the following command to install:

```sh
dotnet tool install --global cloudflare-ddns
```

Or use the following to upgrade to the latest version:

```sh
dotnet tool update --global cloudflare-ddns
```

The tool should now be available from your favourite terminal, just give it a try with `cloudflare-ddns --help`.

It's advised to run this program at repeated intervals. To get this up and running please refer to whatever is available on your current OS. The following will walk you through setting up a repeated task on Windows and Linux:

#### Linux
To get this up and running in a local Linux setup you will need to combine this tool with crontab. The following steps will get you up and running in no time:

1. Create a config file somewhere on your distro.

2. Run the following code in terminal

```bash
crontab -e
```

3. Add the following lines to sync your DNS records every 15 minutes

```bash
*/15 * * * * cloudflare-ddns -c /path/to/your/config-file/config.json
```

This will schedule a cronjob and makes sure your DNS is synced every 15 minutes.

#### Windows
TODO

### Helm
A helm chart is available in the `charts` folder, for more information about the available values, have a look at the README inside the [chart](charts/README.md). For helm deployments you don't need to provide a `config.json`, this will get generated for you. At the moment there is no `helm install` available yet, this is on my TO-DO list though. To get started with helm clone the repository or copy over the files in the charts folder.

### Authentication
To authenticate you will need to create a Cloudflare API token, traditional API keys are not supported! To generate a new API token, go to your [Cloudflare Profile](https://dash.cloudflare.com/profile/api-tokens) and create a token capable of **Edit DNS**. There are 2 ways you can provide this API token:

1. Add an environment variable `CLOUDFLARE_API_TOKEN` with the value of your token.
2. Add a `apiToken` key at the root of your config.json.

### Configuration
The tool can be configured with a `config.json` file which by default will get picked up at the cwd. It's also possible to change the path or name to the configuration file via `-c` or `--config`. The file looks as follows:

```json
{
  "ipv4Resolver": "<http endpoint to ipv4 resolver>",
  "ipv6Resolver": "<http endpoint to ipv6 resolver>",
  "apiToken": "<cloudflare api token>",
  "dns": [
    {
      "zoneId": "<cloudflare zone id>",
      "domain": "<subdomain name>",
      "proxied": true
    },
    ...
  ]
}
```

- `ipv4Resolver`: optional http endpoint to a service that can resolve your home ipv4 address, defaults to `https://api.ipify.org`.
- `ipv6Resolver`: optional http endpoint to a service that can resolve your home ipv6 address, defaults to `https://api6.ipify.org`.
- `dns.zoneId`: The ID of the zone where the DNS record will be configured. From your dashboard click into the zone. Under the review tab, scroll down and the zone ID is listed on the right.
- `dns.domain`: The subdomain you want to update with an A or AAAA record. IMPORTANT: Only write the subdomain name, do not include the base this is inferred from the zone. (eg foo or an empty string to update the base domain)
- `proxied`: Determines whether request get proxied through Cloudflare, defaults to false. (This usually disables SSH)

## Usage

You can use `--help` to get an overview of accepted arguments:
```
cloudflare-ddns: 0.2.0

Cloudflare DDNS

Usage: cloudflare-ddns [options]

Options

  -c, --config               Path to the config.json file, defaults to the
                               current working directory.
  -l, --log-level=VALUE      Set the log level (verbose, info, warning, error),
                               defaults to info
  -v, --version              Show current version.
  -?, -h, --help             Show help information

```

- `-c` or `--config` allows configuring the path to the config.json file. By default, the tool will look in the current directory for this file.
- `-l` or `--log-level` allows changing the verbosity of the tool. By default, it set to info.

## FAQ

#### Can I use my own IP address resolver?
Yes, you most definitely can. The specification is fairly simple, a successful response should return a 200 status code with the IP address as plain text. Any other status code will get interpreted as an error and thus ignored. There is no example of a resolver at the moment, but one for azure functions is coming soon 😁.

#### How can I best host multple domains from the same IP?
You can save yourself some trouble when hosting multiple domains pointing to the same IP address (in the case of Traefik) by defining one A & AAAA record  'ddns.example.com' pointing to the IP of the server that will be updated by this DDNS script. For each subdomain, create a CNAME record pointing to 'ddns.example.com'. Now you don't have to manually modify the script config every time you add a new subdomain to your site!

## License

Copyright 2020 [Nick Van Dyck](https://nvd.codes)

MIT

[ci-url]: https://github.com/nickvdyck/cloudflare-ddns
[ci-badge]: https://github.com/nickvdyck/cloudflare-ddns/workflows/Main/badge.svg

[nuget-package-url]: https://www.nuget.org/packages/cloudflare-ddns/
[nuget-package-badge]: https://img.shields.io/nuget/v/cloudflare-ddns.svg?style=flat-square&label=cloudflare-ddns

[sonar-cloud-coverage-url]: https://sonarcloud.io/dashboard?id=nickvdyck_cloudflare-ddns
[sonar-cloud-coverage-badge]: https://sonarcloud.io/api/project_badges/measure?project=nickvdyck_cloudflare-ddns&metric=coverage

[feedz-package-url]: https://f.feedz.io/nvd/cloudflare-ddns/packages/cloudflare-ddns/latest/download
[feedz-package-badge]: https://img.shields.io/badge/endpoint.svg?url=https%3A%2F%2Ff.feedz.io%2Fnvd%2Fcloudflare-ddns%2Fshield%2Fcloudflare-ddns%2Flatest&label=cloudflare-ddns
