![Consumer Data Right Logo](https://raw.githubusercontent.com/ConsumerDataRight/mock-data-recipient/main/cdr-logo.png) 

[![Consumer Data Standards 1.11.0](https://img.shields.io/badge/Consumer%20Data%20Standards-v1.11.0-blue.svg)](https://consumerdatastandardsaustralia.github.io/standards/includes/releasenotes/releasenotes.1.11.0.html#v1-11-0-release-notes)
[![Conformance Test Suite 3.2](https://img.shields.io/badge/Conformance%20Test%20Suite-v3.2-darkblue.svg)](https://www.cdr.gov.au/for-providers/conformance-test-suite-data-recipients)
[![made-with-dotnet](https://img.shields.io/badge/Made%20with-.NET-1f425Ff.svg)](https://dotnet.microsoft.com/)
[![made-with-csharp](https://img.shields.io/badge/Made%20with-C%23-1f425Ff.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![MIT License](https://img.shields.io/github/license/ConsumerDataRight/mock-data-recipient)](./LICENSE)
[![Pull Requests Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](./CONTRIBUTING.md)

# Consumer Data Right - Mock Data Recipient
This project includes source code, documentation and instructions for a Consumer Data Right (CDR) Mock Data Recipient.

This repository contains a mock implementation of a Mock Data Recipient and is offered to help the community in the development and testing of their CDR solutions.

## Mock Data Recipient - Alignment
The Mock Data Recipient aligns to [v1.11.0](https://consumerdatastandardsaustralia.github.io/standards/includes/releasenotes/releasenotes.1.11.0.html#v1-11-0-release-notes) of the [Consumer Data Standards](https://consumerdatastandardsaustralia.github.io/standards).
The Mock Data Recipient passed v3.2 of the [Conformance Test Suite for Data Recipients](https://www.cdr.gov.au/for-providers/conformance-test-suite-data-recipients).

## Getting Started
The Mock Data Recipient was built using the [Mock Register](https://github.com/ConsumerDataRight/mock-register) and the [Mock Data Holder](https://github.com/ConsumerDataRight/mock-data-holder). You can swap out any of the Mock Data Holder, Mock Data Register and Mock Data Recipient solutions with a solution of your own.

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

If you would like to contribute features or fixes back to the Mock Data Recipient repository, consult the [contributing guidelines](CONTRIBUTING.md).

### Use the pre-built image

A version of the Mock Data Recipient is built into a single Docker image that is made available via [docker hub](https://hub.docker.com/r/consumerdataright/mock-data-recipient).

The container can simply be run by pulling and running the latest image using the following Docker commands:

#### Pull the latest image

```
docker pull consumerdataright/mock-data-recipient
```

#### Run the Mock Recipient container

```
docker run -d -h mock-data-recipient -p 9001:9001 --name mock-data-recipient consumerdataright/mock-data-recipient
```

#### Certificate Management

Consult the [Certificate Management](CertificateManagement/README.md) documentation for more information about how certificates are used for the Mock Data Recipient.

### Use the docker compose file to run a multi-container mock CDR Ecosystem

The [docker compose file](Source/DockerCompose/docker-compose.yml) can be used to run multiple containers from the Mock CDR Ecosystem.
1. Add the following to your hosts file, eg C:\Windows\System32\drivers\etc\hosts
````
127.0.0.1 mock-data-holder
127.0.0.1 mock-data-recipient
127.0.0.1 mock-register
````
2. Flush the DNS cache, on Windows use: 
````
ipconfig /flushdns
````
3. Run the [docker compose file](Source/DockerCompose/docker-compose.yml)
````
docker-compose up
````

Update the docker compose file if you would like to swap out one of the mock solutions with a solution of your own.

## Mock Data Recipient - Architecture
The following diagram outlines the high level architecture of the Mock Data Recipient:

![Mock Recipient - Architecture](https://raw.githubusercontent.com/ConsumerDataRight/mock-data-recipient/main/mock-data-recipient-architecture.png)

## Mock Data Recipient - Components
The Mock Data Recipient contains the following components:

- Website & API
  - Hosted at `https://localhost:9001`
  - Contains the user interface for testing a variety of interactions with participants, including:
    - Get Data Holder Brands
    - Get SSA
    - Dynamic Client Registation
    - Consent and Authorisation
    - Consumer Data Sharing
    - PAR
  - Also contains the JWKS and CDR Arrangement Revocation endpoints.
- SDK
  - Used internally within the Mock Data Recipient to simplify interactions with the Register and Data Holders.
- Repository
  - An in memory SQLite repository is included that contains local data used within the Mock Data Recipient.
  - Includes the following collections:
    - `data-holder-brands` - populated when a response is received from the Register's `Get Data Holder Brands` API.
    - `client-registrations` - populated when a response is received from a successful DCR request to a Data Holder.
    - `cdr-arrangements` - populated after a successful consent and authorisation flow with a Data Holder.

## Technology Stack
The following technologies have been used to build the Mock Data Recipient:
- The source code has been written in `C#` using the `.NET 5` framework.
- The Repository utilises a `SQLite` instance.

# Testing
The Mock Data Recipient has been built as a test harness to demonstrate the interactions between the Register and Data Holders.  The Mock Data Recipient allows the end user to test the various interactions by changing input values, executing and viewing the response.  The Mock Data Recipient requires a [Mock Register](https://github.com/ConsumerDataRight/mock-register) and a [Mock Data Holder](https://github.com/ConsumerDataRight/mock-data-holder) to completely mimic the CDR Ecosystem. You can swap out any of the Mock Data Holder, Mock Data Register and Mock Data Recipient solutions with a solution of your own.

# Contribute
We encourage contributions from the community.  See our [contributing guidelines](CONTRIBUTING.md).

# Code of Conduct
This project has adopted the **Contributor Covenant**.  For more information see the [code of conduct](CODE_OF_CONDUCT.md).

# License
[MIT License](./LICENSE)

# Notes
The Mock Data Recipient is provided as a development tool only. It conforms to the Consumer Data Standards and Register Design.
