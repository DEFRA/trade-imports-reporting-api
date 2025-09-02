# Trade Imports Reporting API

The Trade Imports Reporting API is a .NET application which subscribes to Data API resource events and exposes reporting information.

* [Prerequisites](#prerequisites)
* [Setup Process](#setup-process)
* [How to run in development](#how-to-run-in-development)
* [How to run Tests](#how-to-run-tests)
* [Running](#running)
* [Deploying](#deploying)
* [SonarCloud](#sonarCloud)
* [Dependabot](#dependabot)
* [Message Consumption](#message-consumption)
* [Tracing](#tracing)
* [Licence Information](#licence-information)
* [About the Licence](#about-the-licence)

### Prerequisites

- .NET 9 SDK
- Docker
  - localstack - used for local queuing
  - wiremock - used for mocking out http requests

### Setup Process

- Install the .NET 9 SDK
- Install Docker
  - Run the following Docker Compose to set up locally running queues for testing
  ```bash
  docker compose -f compose.yml up -d
  ```

### How to run in development

Run the application with the command:

```bash
dotnet run --project src/Api/Api.csproj
```

### How to run Tests

Run the unit tests with:

```bash
dotnet test --filter "Category!=IntegrationTest"
```
Run the integration tests with:
```bash
dotnet test --filter "Category=IntegrationTest"
```
Run all tests with:
```bash
dotnet test
```

#### Unit Tests

Some unit tests may run an in memory instance service.

#### Integration Tests

Integration tests run against the built docker image.

Because these use the built docker image, the `appsettings.json` will be used, should any values need to be overridden, then they can be injected as an environment variable via the `compose.yml`

### Deploying

Before deploying via CDP set any config needed in the appropriate `cdp` app settings JSON in the Api project root, otherwise add as a secret in the CDP portal.

### SonarCloud

See the configured project in SonarCloud.

### Dependabot

We are using dependabot.

Connection to the private Defra nuget packages is provided by a user generated PAT stored in this repo's settings - /settings/secrets/dependabot - see `DEPENDABOT_PAT` secret.

The PAT is a classic token and needs permissions of `read:packages`.

At time of writing, using PAT is the only way to make Dependabot work with private nuget feeds.

Should the user who owns the PAT leave Defra then another user on the team should create a new PAT and update the settings in this repo.

### Message Consumption

This service is using a framework called [Slim Message Bus](https://github.com/zarusz/SlimMessageBus), which maps queues/types to consumer classes.

### Tracing

The out of the box CDP template doesn't provide any example of how to handle tracing for non Http communication.

This service expects the `trace.id` to be a header on the message.

Getting the `trace.id` header is achieved via a SMB `TraceContextInterceptor`.

Making sure that `trace.id` is then used in log messages is achieved via `TraceContextEnricher`.

Setting the `trace.id` header on a HTTP request is achieved via Header Propagation.

### Licence Information

THIS INFORMATION IS LICENSED UNDER THE CONDITIONS OF THE OPEN GOVERNMENT LICENCE found at:

<http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3>

### About the licence

The Open Government Licence (OGL) was developed by the Controller of Her Majesty's Stationery Office (HMSO) to enable information providers in the public sector to license the use and re-use of their information under a common open licence.

It is designed to encourage use and re-use of information freely and flexibly, with only a few conditions.