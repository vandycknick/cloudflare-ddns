# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## Unreleased
### Added
- Allow setting api token via environment variable CLOUDFLARE_API_TOKEN
- Allow configuring log-level via cli argument --log-level (verbose, info, warning, error)

### Changed
- apiKey in config file renamed to apiToken to align naming with cloudflare. (Breaking)

## [v0.1.0] - 2020-11-11
- Initial release
