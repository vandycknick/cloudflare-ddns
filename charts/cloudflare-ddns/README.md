# cloudflare-ddns

Installs cloudflare-ddns in kubernetes

Source code can be found [here](https://github.com/nickvdyck/cloudflare-ddns)

## Configuration

The following table lists the configurable parameters of the pihole chart and the default values.

## Chart Values

| Key              | Type   | Default                               | Description                                                                                        |
| ---------------- | ------ | ------------------------------------- | -------------------------------------------------------------------------------------------------- |
| image.repository | string | `"ghcr.io/nickvdyck/cloudflare-ddns"` |                                                                                                    |
| image.tag        | string | `"v0.1.0"`                            |                                                                                                    |
| image.pullPolicy | string | `"IfNotPresent"`                      |                                                                                                    |
| schedule         | string | `"*/15 * * * *"`                      |                                                                                                    |
| secrets.apiToken | string | `""`                                  | Cloudflare personal api token.                                                                     |
| history.success  | int    | `3`                                   | Number of successful finished jobs to retain.                                                      |
| history.failed   | int    | `1`                                   | Number of failed finished jobs to retain.                                                          |
| resources        | object | `{}`                                  |                                                                                                    |
| logging.level    | string | `"info"`                              | Log level (verbose, info, warning error).                                                          |
| config.ipv4      | bool   | `true`                                | Public IPv4 lookup enabled.                                                                        |
| config.ipv6      | bool   | `true`                                | Public IPv6 lookup enabled.                                                                        |
| config.apiToken  | string | `""`                                  | Cloudflare personal token, do not use this in k8s.                                                 |
| config.records   | list   | `[]`                                  | Subdomain names to sync, look at cloudflare-ddns `config.json` specification for more information. |

### Example

Sets up cloudflare ddns to run every minute and sync a single domain name into a given zone.

```yaml
image:
  tag: v0.1.0

schedule: "*/1 * * * *"

secrets:
  apiToken: "123"

config:
  records:
    - zoneId: "456"
      subdomain: "ddns"
      proxied: true
```
