# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [v1.0.0] - 2022-03-18

### Changed

- Updated to dotnet 6.0
- Updated dependencies to latest version

## [v0.4.0] - 2020-12-06

### Fixed

- Resolve public ip with cloudflare DNS not working because cloudflare uses Chaos class.

## [v0.3.0] - 2020-11-14

### Added

- Ability to resolve public ip over DNS (cloudflare, google)
- Ability to switch between DNS or HTTP resolvers
- Ability to fallback to another resolver when the first one is not able to resolve a public IP.
- Ability to disable resolving public ipv4 or ipv6 address

### Changed

- Changed config file specification (Breaking).

## [v0.2.0] - 2020-11-11

### Added

- Allow setting api token via environment variable CLOUDFLARE_API_TOKEN
- Allow configuring log-level via cli argument --log-level (verbose, info, warning, error)
- Added helm chart for k8s deployments

### Changed

- apiKey in config file renamed to apiToken to align naming with cloudflare. (Breaking)

## [v0.1.0] - 2020-11-11

- Initial release
