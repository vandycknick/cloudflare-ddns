# Cloudflare DDNS

[![Build status][ci-badge]][ci-url]
[![NuGet][nuget-package-badge]][nuget-package-url]
[![feedz.io][feedz-package-badge]][feedz-package-url]
[![Coverage][sonar-cloud-coverage-badge]][sonar-cloud-coverage-url]

> Dynamic DNS service based on Cloudflare! Access your home network remotely via a custom domain name without a static IP!

## What it does

Cloudflare DDNS allows you to run your own Dynamic DNS service from the comfort of your own infrastructure. Combined with a Raspberry PI or whatever spare hardware you still have lying around, it helps promote a more decentralized internet at a low cost. During execution, it resolves your current public IPv4 and IPv6 addresses and creates/updates DNS records in Cloudflare to point to your home address. It keeps track of any DNS records that it is managing, giving you some peace of mind and allowing it to run in non-empty Cloudflare zones without having to worry that your public blog gets taken down. Any stale or duplicate records will get safely cleaned up.

## Getting started

### Local Installation

Download [.NET Core 3.1](https://dotnet.microsoft.com/download) or newer. Once installed, run this command:

```sh
dotnet tool install --global cloudflare-ddns
```

Setup your dns records and use the following to start syncing your public ip to cloudflare

```sh
cloudflare-ddns
```

It's advised to run this program at repeated intervals. The following will walk you through setting up a recurring task on Windows and Linux:

#### Linux

On Linux you can leverage crontab to schedule this command at a recurring interval:

1. Create a config file somewhere on your distro.

2. Run the following code in terminal, this will launch your default editor with the crontab config file

```bash
crontab -e
```

3. Add the following lines to sync your DNS records every 15 minutes

```bash
*/15 * * * * cloudflare-ddns -c /path/to/your/config-file/config.json
```

This will schedule a cronjob and makes sure your DNS is synced every 15 minutes.

#### Windows

1. Create a config file somewhere on your distro.

2. Open `Task Scheduler` and create a task.

3. In General tab check `Run whether user is logged on or not`

4. In Triggers tab check `Repeat task every` and select `5 min`.

5. In Action tab add an action.
    * the `program` is `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe`
    * the `arguments` is `cloudflare-ddns`
    * the `start in` is where your config file location in step 1.

6. Click OK and wait last result become `0x0`. if not,check config and `cloudflare-ddns` command works ok in powershell.

### Helm

A helm chart is available in the `charts` folder, for more information about the available values, have a look at the README inside the [chart](charts/cloudflare-ddns/README.md). For helm deployments you don't need to provide a `config.json`, this will get generated for you. At the moment there is no `helm install` available yet, this is on my TO-DO list though. To get started with helm clone the repository or copy over the files in the charts folder.

### Authentication

To authenticate you will need to create a Cloudflare API token, traditional API keys are not supported! To generate a new API token, go to your [Cloudflare Profile](https://dash.cloudflare.com/profile/api-tokens) and create a token capable of **Edit DNS**. There are 2 ways you can provide this API token:

1. Add an environment variable `CLOUDFLARE_API_TOKEN` with the value of your token.
2. Add an `apiToken` key at the root of your config.json.

### Configuration

The tool can be configured with a `config.json` file which by default will get picked up at the cwd. It's also possible to change the path or name to the configuration file via `-c` or `--config`. A minimal valid config file looks as follows:

```json
{
  "records": [
    {
      "zoneId": "<cloudflare_zone_id>",
      "subdomain": "<subdomain_name>",
      "proxied": true
    },
    ...
  ]
}
```

For a minimal valid configuration, all you need to define are the records which you would like to sync. But there are a few more options available to play with:

```json
{
  "apiToken": "<cloudflare_api_token>",
  "ipv4": true,
  "ipv6": true,
  "resolvers": {
    "http": {
      "ipv4Endpoint": "https://api.ipify.org",
      "ipv6Endpoint": "https://api6.ipify.org"
    },
    "dns": "cloudflare",
    "order": [
      "http",
      "dns"
    ]
  },
  "records": [
    {
      "zoneId": "<cloudflare_zone_id>",
      "subdomain": "<subdomain_name>",
      "proxied": true
    },
    {
      "zoneId": "<cloudflare_zone_id>",
      "subdomain": "<subdomain_name>",
      "proxied": false
    },
    ...
  ]
}
```

#### Config.json Values

_Config:_

| Key                         | Type   | Default                    | Description                                                                                                                                                                                                                                                                                                              |
| --------------------------- | ------ | -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| apiToken                    | string | `""`                       | Prefer using `CLOUDFLARE_API_TOKEN` environment variable instead!! Cloudflare API token required to talk to the Cloudflare REST API, traditional API keys are not supported! Make sure this token has the correct permissions for each zone that it needs to create a record. Have a look at the Authentication section! |
| ipv4                        | bool   | `true`                     | Determines if public ipv4 should get resolved.                                                                                                                                                                                                                                                                           |
| ipv6                        | bool   | `true`                     | Determines if public ipv6 should get resolved.                                                                                                                                                                                                                                                                           |
| resolvers.http.ipv4Endpoint | string | `"https://api.ipify.org"`  | HTTP REST endpoint for HTTP resolver to determine public ipv4 address.                                                                                                                                                                                                                                                   |
| resolvers.http.ipv6Endpoint | string | `"https://api6.ipify.org"` | HTTP REST endpoint for HTTP resolver to determine public ipv6 address.                                                                                                                                                                                                                                                   |
| resolvers.dns               | string | `"cloudflare"`             | DNS nameserver to use to resolve public ipv4 or/and ipv6 addresses. Supported servers are `cloudflare` and `google`.                                                                                                                                                                                                     |
| resolvers.order             | list   | `["http", "dns"]`          | Determines the order in which resolvers will be used. By default, it will first try to resolve an IP via HTTP and fallback to DNS. It's possible to change the order and even leave out a specific resolver if you for example only want to resolve over HTTP. An empty list is not supported!                           |
| resources                   | object | `{}`                       |                                                                                                                                                                                                                                                                                                                          |
| records                     | list   | `[]`                       | List of subdomains to sync in a given Cloudflare zone eg.                                                                                                                                                                                                                                                                |

_Record:_

| Key       | Type   | Default | Description                                                                                                                                                                                                        |
| --------- | ------ | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| zoneId    | string | `""`    | The ID of the zone where the DNS record will be configured. From your dashboard click into the zone. Under the review tab, scroll down and the zone ID is listed on the right.                                     |
| subdomain | string | `""`    | The subdomain you want to update with an A or AAAA record. IMPORTANT: Only write the subdomain name, do not include the base this is inferred from the zone. (eg foo or an empty string to update the base domain) |
| proxied   | bool   | `false` | Determines whether requests get proxied through Cloudflare. (This usually disables SSH).                                                                                                                           |

## Usage

You can use `--help` to get an overview of accepted arguments:

```
cloudflare-ddns: 0.3.0

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

#### How can I best host multiple subdomains from the same IP?

You can save yourself some trouble when hosting multiple domains pointing to the same IP address (in the case of Traefik) by defining one A & AAAA record 'ddns.example.com' pointing to the IP of the server that will be updated by this DDNS script. For each subdomain, create a CNAME record pointing to 'ddns.example.com'. Now you don't have to manually modify the script config every time you add a new subdomain to your site!

#### I keep getting errors related to resolving my public IPv6 address?

When running in verbose mode you will get a richer logging experience. And thus will see lines telling you if it can't resolve an IPv4 or IPv6 address.
If you are running in an environment that only uses IPv4 (for example docker or a default k8s setup). Then you won't be able to resolve your public IPv6 address and that's why you will see these error messages. If you for certain know that you can't resolve IPv6 in your environment or don't need your public IPV6 to be synced, then you can turn this off in your `config.json` file:

```json
{
  ...
  "ipv6": false,
  ...
}
```

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
