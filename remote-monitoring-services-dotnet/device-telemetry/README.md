[![Build][build-badge]][build-url]
[![Issues][issues-badge]][issues-url]
[![Gitter][gitter-badge]][gitter-url]

# Device Telemetry Overview

This service provides a RESTful endpoint for read access to device
telemetry, full CRUD for rules, and read/write for alarms from storage.

## Why?

This microservice was built as part of the
[Azure IoT Remote Monitoring](https://github.com/Azure/azure-iot-pcs-remote-monitoring-dotnet)
project to provide a generic implementation for an end-to-end IoT solution. More information [here][rm-arch-url].

## Features
* Gets a list of telemetry messages
* Gets a list of alarms
* Gets a single alarm
* Modifies alarm status
* Create/Read/Update/Delete Rules

## Documentation

* View the API documentation in the
[Wiki](https://github.com/Azure/remote-monitoring-services-dotnet/wiki/Telemetry-API).

# How to use

## Running the service with Docker

You can run the microservice and its dependencies using
[Docker](https://www.docker.com/) with the instructions [here][run-with-docker-url].

## Running the service locally

## Prerequisites
### 1. Deploy Azure Services
This service has a dependency on the following Azure resources.
Follow the instructions for
[Deploy the Azure services](https://docs.microsoft.com/azure/iot-suite/iot-suite-remote-monitoring-deploy-local#deploy-the-azure-services).
* Cosmos DB

### 2. Setup Dependencies

This service depends on the following repository.
1. [Storage Adapter Microservice](https://github.com/Azure/remote-monitoring-services-dotnet/tree/master/storage-adapter)
2. [Authentication Microservice](https://github.com/Azure/remote-monitoring-services-dotnet/tree/master/auth)

### 3. Environment variables required to run the service
In order to run the service, some environment variables need to be created
at least once. See specific instructions for IDE or command line setup below
for more information. More information on environment variables
[here](#configuration-and-environment-variables).
 
* `PCS_AAD_APPID` = { Azure service principal id }
* `PCS_AAD_APPSECRET` = { Azure service principal secret }
* `PCS_KEYVAULT_NAME` = { Name of Key Vault resource that stores settings and configuration }

### 3.1 Configuration values used from Key Vault
Some of the configuration needed by the microservice is stored in an instance of Key Vault that was created on initial deployment. The telemetry microservice uses:

  * `documentDBConnectionString ` = {your Azure Cosmos DB connection string}
  * `storageAdapterWebServiceUrl ` = http://localhost:9022/v1
  * `authWebServiceUrl` = http://localhost:9001/v1
  * `aadTenantId` = {Azure Active Directory Tenant ID}
    * see: Azure Portal => Azure Active Directory => Properties => Directory ID
  * `aadAppId` = {Azure Active Directory application ID}
    * see: Azure Portal => Azure Active Directory => App Registrations => Your App => Application ID
  * `aadAppSecret` = {application secret}
    * see: Azure Portal => Azure Active Directory => App Registrations => Your App => Settings => Passwords
  * `telemetryStorageType` = "tsi"
    * Allowed values: ["cosmosdb", "tsi"]. Default is "tsi"
  * `tsiDataAccessFQDN` = {Time Series FQDN}
    * see: Azure Portal => Your Resource Group => Time Series Insights Environment => Data Access FQDN
  * `diagnosticsWebServiceUrl` (optional) = http://localhost:9006/v1
  * `actionsEventHubName` = {Event hub name}
  * `actionsEventHubConnectionString` = {Endpoint=sb://....servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=...}
    * see: Azure Portal => Your resource group => your event hub namespace => Shared access policies
  * `logicAppEndpointUrl` = {Logic App Endpoint}
    * see: Azure Portal => Your resource group => Your Logic App => Logic App Designer => When a Http Request is received => HTTP POST URL
  * `storageConnectionString` = {connection string}
    * see: Azure Portal => Your resource group => Your Storage Account => Access keys => Connection String
  * `solutionWebsiteUrl` = {Solution Url}
  * `corsWhitelist` = Specifies where requests are allowed from "{ 'origins': ['\*'], 'methods': ['\*'], 'headers': ['\*'] }" to allow everything. Empty to disable CORS


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
1. Using an HTTP client like [Postman][postman-url], use the
[RESTful API][project-wiki] to test out the service.

## Running the service from the command line

1. Make sure the [Prerequisites](#prerequisites) are set up.
1. Set the following environment variables in your system.
More information on environment variables
[here](#configuration-and-environment-variables).
    1. `PCS_AAD_APPID` = { Azure service principal id }
    1. `PCS_AAD_APPSECRET` = { Azure service principal secret }
    1. `PCS_KEYVAULT_NAME` = { Name of Key Vault resource that stores settings and configuration }
1. Use the scripts in the [scripts](scripts) folder for many frequent tasks:
   * `build`: compile all the projects and run the tests.
   * `compile`: compile all the projects.
   * `run`: compile the projects and run the service. This will prompt for
  elevated privileges in Windows to run the web service.

## Project Structure
This microservice contains the following projects:
* **WebService.csproj** - C# web service exposing REST interface for managing Ruels,
    Alarms, and Messages
* **WebService.Test.csproj** - Unit tests for web services functionality
* **Services.csproj** - C# assembly containining business logic for interacting
with storage and the Storage Adapter microservice
* **Services.Test.csproj** - Unit tests for services functionality
* **Solution/scripts** - Contains build scripts, docker container creation scripts,
and scripts for running the microservice from the command line

## Updating the Docker image

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

Please follow our [contribution guidelines](CONTRIBUTING.md).  We love PRs too.

# Feedback

Please enter issues, bugs, or suggestions as
[GitHub Issues](https://github.com/Azure/device-telemetry-dotnet/issues).

# License

Copyright (c) Microsoft Corporation. All rights reserved.
Licensed under the [MIT](LICENSE) License.

[build-badge]:https://solutionaccelerators.visualstudio.com/RemoteMonitoring/_apis/build/status/Consolidated%20Repo
[build-url]: https://solutionaccelerators.visualstudio.com/RemoteMonitoring/_build/latest?definitionId=22
[issues-badge]: https://img.shields.io/github/issues/azure/device-telemetry-dotnet.svg
[issues-url]: https://github.com/Azure/remote-monitoring-services-dotnet/issues
[gitter-badge]: https://img.shields.io/gitter/room/azure/iot-solutions.js.svg
[gitter-url]: https://gitter.im/azure/iot-solutions
[project-wiki]: https://github.com/Azure/remote-monitoring-services-dotnet/wiki/Telemetry-API
[postman-url]: https://www.getpostman.com
[dotnet-install]: https://www.microsoft.com/net/learn/get-started
[vs-install-url]: https://www.visualstudio.com/downloads
[dotnetcore-tools-url]: https://www.microsoft.com/net/core#windowsvs2017
[omnisharp-url]: https://github.com/OmniSharp/omnisharp-vscodedowsvs2017
[windows-envvars-howto-url]: https://superuser.com/questions/949560/how-do-i-set-system-environment-variables-in-windows-10
[docker-compose-install-url]: https://docs.docker.com/compose/install
[run-with-docker-url]:https://docs.microsoft.com/azure/iot-suite/iot-suite-remote-monitoring-deploy-local#run-the-microservices-in-docker
[rm-arch-url]:https://docs.microsoft.com/azure/iot-suite/iot-suite-remote-monitoring-sample-walkthrough
