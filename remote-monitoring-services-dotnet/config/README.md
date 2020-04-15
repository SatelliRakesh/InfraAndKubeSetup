
[![Build][build-badge]][build-url]
[![Issues][issues-badge]][issues-url]
[![Gitter][gitter-badge]][gitter-url]

# Config Service Overview

This service handles communication with the [Storage Adapter] microservice to complete tasks.

The microservice provides a RESTful endpoint to make CRUD operations for
"devicegroups", "solution-settings", and "user-settings".
The data will be stored by the [Storage Adapter] microservice.

## Why?

This microservice was built as part of the 
[Azure IoT Remote Monitoring](https://github.com/Azure/azure-iot-pcs-remote-monitoring-dotnet)
project to provide a generic implementation for an end-to-end IoT solution.
More information [here][rm-arch-url].

## Features
* Create or update device groups
* Get all or a single device group
* Get or upload logo
* Get or set overall solution settings
* Get or set individual user settings
* Create or delete a package
* Get all or a single uploaded package.

## Documentation
* View the API documentation in the [Wiki](https://github.com/Azure/remote-monitoring-services-dotnet/wiki/Config-Api)

# How to Use

## Running the Service with Docker
You can run the microservice and its dependencies using
[Docker](https://www.docker.com/) with the instructions
[here][run-with-docker-url].

## Running the Service Locally
## Prerequisites

### 1. Deploy Azure Services

This service has a dependency on the following Azure resources. 
Follow the instructions for 
[Deploy the Azure services](https://docs.microsoft.com/azure/iot-suite/iot-suite-remote-monitoring-deploy-local#deploy-the-azure-services).

* Cosmos DB
* Iot Hub
* Maps (optional)

### 2. Setup Dependencies

This service depends on the following repositories.
Run those services from the instructions in their READMEs in the following order.

1. [Storage Adapter Dotnet Microservice](https://github.com/Azure/remote-monitoring-services-dotnet/tree/master/storage-adapter)
1. [Telemetry Dotnet Microservice](https://github.com/Azure/remote-monitoring-services-dotnet/tree/master/device-telemetry)
1. [Device Simulation Dotnet Microservice](https://github.com/Azure/remote-monitoring-services-dotnet/tree/master/device-simulation)
1. [Authentication Microservice](https://github.com/Azure/remote-monitoring-services-dotnet/tree/master/auth)

### 3. Environment variables required to run the service
In order to run the service, some environment variables need to be created 
at least once. See specific instructions for IDE or command line setup below
for more information. More information on environment variables
[here](#configuration-and-environment-variables).

* `PCS_AAD_APPID` = { Azure service principal id }
* `PCS_AAD_APPSECRET` = { Azure service principal secret }
* `PCS_KEYVAULT_NAME` = { Name of Key Vault resource that stores settings and configuration }

## Configuration values used from Key Vault
Some of the configuration needed by the microservice is stored in an instance of Key Vault that was created on initial deployment. The config microservice uses:

* `aadTenantId` = GUID representing your active directory tenant
* `armEndpointUrl` = The endpoint for the Azure Resource Manager API
* `authIssuer` = Identifies the security token service (STS) i.e. https://sts.windows.net/tenantId/ 
* `authRequired` = Whether or not authentication is needed for calls to microservices i.e. from the web ui or postman
* `authWebServiceUrl` = Endpoint for the remote monitoring auth microservice
* `azureMapsKey` = Key needed for Azure Maps Account
* `corsWhitelist` = Specifies where requests are allowed from "{ 'origins': ['\*'], 'methods': ['\*'], 'headers': ['\*'] }" to allow everything. Empty to disable CORS
* `deviceSimulationWebServiceUrl` = Endpoint for device simulation microservice
* `office365ConnectionUrl` = Office 365 url used for enabling e-mail actions when alerting on rules
* `seedTemplate` = The template used by device simulation when seeding devices. This is typically DEFAULT
* `solutionName` = The name of the RM solution that was created. This is typically the same as your Resource Group name
* `solutionType` = Type of solution. remotemonitoring or device-simulation 
* `storageAdapterWebServiceUrl` = Endpoint for storage adapter microservice
* `subscriptionId` = GUID that uniquely identifies your subscription to use Azure services
* `telemetryWebServiceUrl` = Endpoint for telemetry microservice

## Running the service with Visual Studio or VS Code

1. Make sure the [Prerequisites](#prerequisites) are set up.
1. [Install .NET Core 2.x][dotnet-install]
1. Install any recent edition of Visual Studio (Windows/MacOS) or Visual
   Studio Code (Windows/MacOS/Linux).
   * If you already have Visual Studio installed, then ensure you have
   [.NET Core Tools for Visual Studio 2017][dotnetcore-tools-url]
   installed (Windows only).
   * If you already have VS Code installed, then ensure you have the [C# for Visual Studio Code (powered by OmniSharp)][omnisharp-url] extension installed.
1. Open the solution in Visual Studio or VS Code.
1. Define the following environment variables. See [Configuration and Environment variables](#configuration-and-environment-variables) for detailed information for setting these for your enviroment.
    1. `PCS_AAD_APPID` = { Azure service principal id }
    1. `PCS_AAD_APPSECRET` = { Azure service principal secret }
    1. `PCS_KEYVAULT_NAME` = { Name of Key Vault resource that stores settings and configuration }
1. Start the WebService project (e.g. press F5).
1. Use an HTTP client such as [Postman][postman-url], to exercise the
   [RESTful API](https://github.com/Azure/pcs-config-dotnet/wiki/API-Specs).

## Running the service from the command line

1. Make sure the [Prerequisites](#prerequisites) are set up.
1. Set the following environment variables in your system. 
More information on environment variables
[here](#configuration-and-environment-variables).
    1. `PCS_AAD_APPID` = { Azure service principal id }
    1. `PCS_AAD_APPSECRET` = { Azure service principal secret }
    1. `PCS_KEYVAULT_NAME` = { Name of Key Vault resource that stores settings and configuration values }
1. Use the scripts in the [scripts](scripts) folder for many frequent tasks:
   *  `build`: compile all the projects and run the tests.
   *  `compile`: compile all the projects.
   *  `run`: compile the projects and run the service. This will prompt for
  elevated privileges in Windows to run the web service.

## Project Structure
This microservice contains the following projects:
* **WebService.csproj** - C# web service exposing REST interface for config functionality
* **WebService.Test.csproj** - Unit tests for web services functionality
* **Services.csproj** - C# assembly containining business logic for interacting 
with storage microserivce, telemetry microservice and device simulation microservice
* **Services.Test.csproj** - Unit tests for services functionality
* **Solution/scripts** - Contains build scripts, docker container creation scripts, 
and scripts for running the microservice from the command line

# Updating the Docker image
The `scripts` folder includes a [docker](scripts/docker) subfolder with the files
required to package the service into a Docker image:

* `Dockerfile`: docker images specifications
* `build`: build a Docker container and store the image in the local registry
* `run`: run the Docker container from the image stored in the local registry
* `content`: a folder with files copied into the image, including the entry point script

## Configuration and Environment variables

The service configuration is stored using ASP.NET Core configuration
adapters, in [appsettings.ini](WebService/appsettings.ini). The INI
format allows to store values in a readable format, with comments.

Configuration in appsettings.ini are typically set in 3 different ways:

1. Environment variables as is the case with ${PCS_AAD_APPID}. This is typically
only done with the 3 variables described above as these are needed to access Key Vault. 
More details about setting environment variables are located below.
1. Key Vault: A number of the settings in this file will be blank as they are expecting
to get their value from a Key Vault secret of the same name.
1. Direct Value: For some values that aren't typically changed or for local development
you can set the value directly in the file.

Depending on the OS and the IDE used, there are several ways to manage environment variables.

1. If you're using Visual Studio or Visual Studio for Mac, the environment
   variables are loaded from the project settings. Right click on WebService,
   and select Options/Properties, and find the section with the list of env
   vars. See [WebService/Properties/launchSettings.json](WebService/Properties/launchSettings.json).
1. Visual Studio Code loads the environment variables from
   [.vscode/launch.json](.vscode/launch.json)
1. When running the service **with Docker** or **from the command line**, the
   application will inherit environment variables values from the system. 
   * Depending on OS and terminal, there are different ways to persist values
     globally, for more information these pages should help:
     * https://superuser.com/questions/949560/
     * https://stackoverflow.com/questions/13046624/how-to-permanently-export-a-variable-in-linux
     * https://stackoverflow.com/questions/135688/setting-environment-variables-in-os-x
     * https://help.ubuntu.com/community/EnvironmentVariables
1. IntelliJ Rider: env. vars can be set in each Run Configuration, similarly to
  IntelliJ IDEA (https://www.jetbrains.com/help/idea/run-debug-configuration-application.html)

# Contributing to the solution
Please follow our [contribution guildelines](CONTRIBUTING.md) and code style
conventions.

# Feedback
Please enter issues, bugs, or suggestions as 
[GitHub Issues](https://github.com/Azure/remote-monitoring-services-dotnet/issues).

# License
Copyright (c) Microsoft Corporation. All rights reserved.
Licensed under the [MIT](LICENSE) License.

[build-badge]:https://solutionaccelerators.visualstudio.com/RemoteMonitoring/_apis/build/status/Consolidated%20Repo
[build-url]: https://solutionaccelerators.visualstudio.com/RemoteMonitoring/_build/latest?definitionId=22
[issues-badge]: https://img.shields.io/github/issues/azure/pcs-config-dotnet.svg
[issues-url]: https://github.com/azure/pcs-config-dotnet/issues
[gitter-badge]: https://img.shields.io/gitter/room/azure/iot-solutions.js.svg
[gitter-url]: https://gitter.im/azure/iot-solutions
[windows-envvars-howto-url]: https://superuser.com/questions/949560/how-do-i-set-system-environment-variables-in-windows-10
[Storage Adapter]:https://github.com/Azure/remote-monitoring-services-dotnet/tree/master/storage-adapter
[rm-arch-url]:https://docs.microsoft.com/en-us/azure/iot-suite/iot-suite-remote-monitoring-sample-walkthrough
[run-with-docker-url]:https://docs.microsoft.com/azure/iot-suite/iot-suite-remote-monitoring-deploy-local#run-the-microservices-in-docker
[postman-url]: https://www.getpostman.com

[dotnet-install]: https://www.microsoft.com/net/learn/get-started
[vs-install-url]: https://www.visualstudio.com/downloads
[dotnetcore-tools-url]: https://www.microsoft.com/net/core#windowsvs2017
[omnisharp-url]: https://github.com/OmniSharp/omnisharp-vscode
