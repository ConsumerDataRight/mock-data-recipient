# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.4] - 2023-08-22
### Fixed
- Refactored code and fixed code smells

## [1.2.3] - 2023-07-27
### Fixed
- Bug limiting the amount of DCR messages that can be queued. [Sandbox Issue 31](https://github.com/ConsumerDataRight/sandbox/issues/31)  

## [1.2.2] - 2023-06-20
### Changed
- Regenerated all mTLS, SSA and TLS certificates to allow for another five years before they expire.

## [1.2.1] - 2023-06-07
### Changed 
- redirect_uris and response_types in DCR request are now array of string.
- support for acr values

## [1.2.0] - 2023-03-21
### Changed 
- Updated to be compliant with FAPI 1.0 phase 3.
- Dynamic Client Registration Azure Function updated to be compliant with FAPI 1.0 phase 3.
- Home page html content to reflect latest version

## [1.1.3] - 2022-11-24
### Fixed 
- Add body to post requests. [Sandbox Issue 15](https://github.com/ConsumerDataRight/sandbox/issues/15)

## [1.1.2] - 2022-10-27
### Fixed 
- Fixed data conversion error when saving Cdr Arrangement. [Sandbox Issue 14](https://github.com/ConsumerDataRight/sandbox/issues/14)

## [1.1.1] - 2022-10-19
### Fixed
- Send redirect_uris as an array instead of string when performing background Dynamic Client Registration requests. [Issue 50](https://github.com/ConsumerDataRight/mock-data-recipient/issues/50)
- Changed CdrArrangement.ClientId database column data type from UniqueIdentifier to nvarchar(100).
- Intermittent issue where redirect_uri in consent and PAR JWT is null. [Sandbox Issue 11](https://github.com/ConsumerDataRight/sandbox/issues/11)
- mtls_endpoint_aliases not used as per RFC 8705 in PAR and token exchange. [Sandbox Issue 12](https://github.com/ConsumerDataRight/sandbox/issues/12)

## [1.1.0] - 2022-10-05
### Added
- Logging middleware to create a centralised list of all API requests and responses

### Changed
- Primary key constraints for registrations. Now combines Client Id and Data Holder Brand Id as the unique constraint.

## [1.0.1] - 2022-08-30
### Changed
- Updated arrangement revocation to match CDS v1.18. Configuration added based on the date to make functionality available or unavailable.
- Updated side menu layout and text on screens.
- Updated package references.

### Fixed
- Fixed issue with Dynamic Client Registration Azure function not retrying for DCR Failed data holders.

## [1.0.0] - 2022-07-22
### Added
- Azure functions to perform Data Holder discovery by polling the Get Data Holder Brands API of the Register.

### Changed
- First version of the Mock Data Recipient deployed into the CDR Sandbox.

## [0.2.1] - 2022-06-09
### Changed
- Build and Test action to archive test results. End to end tests now included in test report.

## [0.2.0] - 2022-05-25
### Added
- PKCE support added to "Consent and Authorisations" and "PAR" in preparation for FAPI 1.0.
- Ability to connect and authenticate with data holders that have implemented either FAPI 1.0 Migration Phase 1 and Phase 2.
- Ability to connect to and test the [Mock Data Holder Energy](https://github.com/ConsumerDataRight/mock-data-holder-energy) and other Energy Data Holders.

### Changed
- Upgraded the Mock Data Recipient codebase to .NET 6.
- Replaced SQLite database with MSSQL database.
- Changed the TLS certificates for the mock data recipient to be signed by the Mock CDR CA.
- Extra steps detailed for using the solution in visual studio, docker containers and docker compose file.
- Use the latest endpoints from the [Mock Register](https://github.com/ConsumerDataRight/mock-register) that support Banking, Energy and Telco as an industry.
- Quality of Life updates to assist with usability and testing.
- Regenerated all certificates to allow for another year before they expire.

## [0.1.2] - 2022-05-09
### Fixed
- Banking API swagger in data sharing not displaying. [Issue 30](https://github.com/ConsumerDataRight/mock-data-recipient/issues/30) 

## [0.1.1] - 2021-12-07
### Added
- GitHub Actions Workflow for Build, Unit Test, and Integration Test project.
- GitHub Issue config with supporting links to CDR related information.

### Changed
- Minor changes to pipeline appsettings files to support GitHub Actions.
- Minor changes to docker command in the ReadMe. [Issue 25](https://github.com/ConsumerDataRight/mock-data-holder/issues/25)

### Fixed
- Fixed issue calling data holder resource APIs using data recipient proxy. [Issue 26](https://github.com/ConsumerDataRight/mock-data-recipient/issues/26)

## [0.1.0] - 2021-10-01

### Added
- First release of the Mock Data Recipient.
