# cloudflare-ddns

Installs cloudflare-ddns in kubernetes

Source code can be found [here](https://github.com/nickvdyck/cloudflare-ddns)

## Configuration

The following table lists the configurable parameters of the pihole chart and the default values.

## Chart Values

| Key              | Type   | Default                               | Description                                                    |
|------------------|--------|---------------------------------------|----------------------------------------------------------------|
| image.pullPolicy | string | `"IfNotPresent"`                      |                                                                |
| image.repository | string | `"ghcr.io/nickvdyck/cloudflare-ddns"` |                                                                |
| image.tag        | string | `"v0.1.0"`                            |                                                                |
| schedule         | string | `"*/15 * * * *"`                      |                                                                |
| secrets.apiToken | string | `""`                                  | Cloudflare personal api token.                                 |
| history.success  | int    | `3`                                   | Number of successful finished jobs to retain.                  |
| history.failed   | int    | `1`                                   | Number of failed finished jobs to retain.                      |
| resources        | object | `{}`                                  |                                                                |
| config.ipv4      | string | `""`                                  | IPv4 endpoint used to resolve ipv4 addresss, defaults to ipify.|
| config.ipv6      | string | `""`                                  | IPv6 endpoint used to resolve ipv4 addresss, defaults to ipify.|
| config.apiToken  | string | `""`                                  | Cloudflare personal token, do not use this in k8s.             |
| config.dns       | list   | `[]`                                  |                                                                |

### Example

Sets up cloudflare ddns to run every minute and sync a single domain name into a given zone.
```yaml
image:
  tag: v0.1.0

schedule: "*/1 * * * *"

secrets:
  apiToken: "123"

config:
  dns:
    - zoneId: "456"
      domain: "ddns"
      proxied: true
```
