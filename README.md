![Consumer Data Right Logo](https://raw.githubusercontent.com/ConsumerDataRight/mock-data-recipient/main/cdr-logo.png) 

[![Consumer Data Standards v1.17.0](https://img.shields.io/badge/Consumer%20Data%20Standards-v1.17.0-blue.svg)](https://consumerdatastandardsaustralia.github.io/standards/#introduction)
[![Conformance Test Suite 3.2](https://img.shields.io/badge/Conformance%20Test%20Suite-v3.2-darkblue.svg)](https://www.cdr.gov.au/for-providers/conformance-test-suite-data-recipients)
[![made-with-dotnet](https://img.shields.io/badge/Made%20with-.NET-1f425Ff.svg)](https://dotnet.microsoft.com/)
[![made-with-csharp](https://img.shields.io/badge/Made%20with-C%23-1f425Ff.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![MIT License](https://img.shields.io/github/license/ConsumerDataRight/mock-data-recipient)](./LICENSE)
[![Pull Requests Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](./CONTRIBUTING.md)

# Consumer Data Right - Mock Data Recipient
This project includes source code, documentation and instructions for a Consumer Data Right (CDR) Mock Data Recipient.

This repository contains a mock implementation of a Mock Data Recipient and is offered to help the community in the development and testing of their CDR solutions.

## Mock Data Recipient - Alignment
The Mock Data Recipient aligns to [v1.17.0](https://consumerdatastandardsaustralia.github.io/standards/#introduction) of the [Consumer Data Standards](https://consumerdatastandardsaustralia.github.io/standards/#introduction).
The Mock Data Recipient passed v3.2 of the [Conformance Test Suite for Data Recipients](https://www.cdr.gov.au/for-providers/conformance-test-suite-data-recipients).
The Mock Data Recipient can connect to and complete authentication against both [FAPI 1.0 Migration Phase 1 and Phase 2](https://consumerdatastandardsaustralia.github.io/standards/#authentication-flows) compliant data holders.

## Getting Started
The Mock Data Recipient was built using the [Mock Register](https://github.com/ConsumerDataRight/mock-register), the [Mock Data Holder](https://github.com/ConsumerDataRight/mock-data-holder) and the [Mock Data Holder Energy](https://github.com/ConsumerDataRight/mock-data-holder-energy). You can swap out any of the Mock Data Holders, Mock Data Register and Mock Data Recipient solutions with a solution of your own.

There are a number of ways that the artefacts within this project can be used:
1. Build and deploy the source code
2. Use the pre-built image
3. Use the docker compose file to run a multi-container mock CDR Ecosystem

### Build and deploy the source code

To get started, clone the source code.
```
git clone https://github.com/ConsumerDataRight/mock-data-recipient.git
```

````
Set your debug profile to CDR.DataRecipient.Web
````

To get help on launching and debugging the solution, see the [help guide](./Help/debugging/HELP.md).

If you would like to contribute features or fixes back to the Mock Data Recipient repository, consult the [contributing guidelines](CONTRIBUTING.md).

### Use the pre-built image

A version of the Mock Data Recipient is built into a single Docker image that is made available via [docker hub](https://hub.docker.com/r/consumerdataright/mock-data-recipient).

#### Pull the latest image

```
docker pull consumerdataright/mock-data-recipient
```

To get help on launching and debugging the solutions as containers and switching out your solution(s), see the [help guide](./Help/container/HELP.md).

#### Certificate Management

Consult the [Certificate Management](CertificateManagement/README.md) documentation for more information about how certificates are used for the Mock Data Recipient.

### Use the docker compose file to run a multi-container mock CDR Ecosystem

The [docker compose file](Source/DockerCompose/docker-compose.yml) can be used to run multiple containers from the Mock CDR Ecosystem.

**Note:** the [docker compose file](Source/DockerCompose/docker-compose.yml) utilises the Microsoft SQL Server Image from Docker Hub. The Microsoft EULA for the Microsoft SQL Server Image must be accepted to use the [docker compose file](Source/DockerCompose/docker-compose.yml). See the Microsoft SQL Server Image on Docker Hub for more information.

To get help on launching and debugging the solutions as containers and switching out your solution(s), see the [help guide](./Help/container/HELP.md).

## Mock Data Recipient - Architecture
The following diagram outlines the high level architecture of the Mock Data Recipient:

[<img src="https://raw.githubusercontent.com/ConsumerDataRight/mock-data-recipient/main/mock-data-recipient-architecture.png" height='600' width='600' alt="Mock Data Recipient - Architecture"/>](https://raw.githubusercontent.com/ConsumerDataRight/mock-data-recipient/main/mock-data-recipient-architecture.png)

Dynamic Client Registration Interface:

[<img src="mock-data-recipient-dcr-architecture.png"  height='240' width='600' alt="Dynamic Client Registration Interface"/>](mock-data-recipient-dcr-architecture.png)

## Mock Data Recipient - Components
The Mock Data Recipient contains the following components:

- Website & API
  - Hosted at `https://localhost:9001`
  - Contains the user interface for testing a variety of interactions with participants, including:
    - Get Data Holder Brands
    - Get SSA
    - Dynamic Client Registration
    - Consent and Authorisation. Supports [FAPI 1.0 Migration Phase 1](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.16.0/#authentication-flows)
    - Consumer Data Sharing
    - OIDC Authentication can be enabled in appsettings by including the issuer details.
    - Pushed Authorisation Request (PAR). Supports [FAPI 1.0 Migration Phase 2](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.16.0/#authentication-flows)
  - Also contains the JWKS and CDR Arrangement Revocation endpoints.
- SDK
  - Used internally within the Mock Data Recipient to simplify interactions with the Register and Data Holders.
- Azure Functions
  - Azure Functions that can automate the continuous Get Data Holders discovery and Dynamic Client Registration process.
  - For each Data Holder retrieved from the Register, a message will be added to the DynamicClietnRegistration queue. A function listening to the queue, will pick up the message and attempt to register the Data Recipient with the Data Holder.
  - To get help on the Azure Functions, see the [help guide](./Help/azurefunctions/HELP.md).
- Repository
  - A SQL repository is included that contains local data used within the Mock Data Recipient.
  - Includes the following collections:
    - `data-holder-brands` - populated when a response is received from the Register's `Get Data Holder Brands` API.
    - `client-registrations` - populated when a response is received from a successful DCR request to a Data Holder.
    - `cdr-arrangements` - populated after a successful consent and authorisation flow with a Data Holder.

## Technology Stack
The following technologies have been used to build the Mock Data Recipient:
- The source code has been written in `C#` using the `.Net 6` framework.
- The Repository utilises a `SQL` instance.

# Testing
The Mock Data Recipient has been built as a test harness to demonstrate the interactions between the Register and Data Holders.  The Mock Data Recipient allows the end user to test the various interactions by changing input values, executing and viewing the response.  The Mock Data Recipient requires a [Mock Register](https://github.com/ConsumerDataRight/mock-register), a [Mock Data Holder](https://github.com/ConsumerDataRight/mock-data-holder) and a [Mock Data Holder Energy](https://github.com/ConsumerDataRight/mock-data-holder-energy) to completely mimic the CDR Ecosystem. You can [swap out](./containerhelp/HELP.md) any of the Mock Data Holders, Mock Data Register and Mock Data Recipient solutions with a solution of your own. Use the Consents and Authorisation flow when testing against data holders that have implemented FAPI 1.0 Migration Phase 1. Use the Pushed Authorisation Request (PAR) flow when testing against data holders that have implemented FAPI 1.0 Migration Phase 2.

# Contribute
We encourage contributions from the community.  See our [contributing guidelines](CONTRIBUTING.md).

# Code of Conduct
This project has adopted the **Contributor Covenant**.  For more information see the [code of conduct](CODE_OF_CONDUCT.md).

# License
[MIT License](./LICENSE)

# Notes
The Mock Data Recipient is provided as a development tool only. It conforms to the Consumer Data Standards.