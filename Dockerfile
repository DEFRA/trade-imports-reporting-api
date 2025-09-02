# Base dotnet image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Add curl to template.
# CDP PLATFORM HEALTHCHECK REQUIREMENT
RUN apt update && \
    apt upgrade -y && \
    apt install curl -y && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY .config/dotnet-tools.json .config/dotnet-tools.json
COPY .csharpierrc .csharpierrc
COPY .csharpierignore .csharpierignore

RUN dotnet tool restore

COPY src/Api/Api.csproj src/Api/Api.csproj
COPY tests/Testing/Testing.csproj tests/Testing/Testing.csproj
COPY tests/TestFixtures/TestFixtures.csproj tests/TestFixtures/TestFixtures.csproj
COPY tests/Api.Tests/Api.Tests.csproj tests/Api.Tests/Api.Tests.csproj
COPY tests/Api.IntegrationTests/Api.IntegrationTests.csproj tests/Api.IntegrationTests/Api.IntegrationTests.csproj
COPY Defra.TradeImportsReportingApi.sln Defra.TradeImportsReportingApi.sln
COPY Directory.Build.props Directory.Build.props

COPY NuGet.config NuGet.config
ARG DEFRA_NUGET_PAT

RUN dotnet restore

COPY src/Api src/Api
COPY tests/Testing tests/Testing
COPY tests/TestFixtures tests/TestFixtures
COPY tests/Api.Tests tests/Api.Tests

RUN dotnet csharpier check .

RUN dotnet build src/Api/Api.csproj --no-restore -c Release

RUN dotnet test --no-restore tests/Api.Tests

FROM build AS publish

RUN dotnet publish src/Api -c Release -o /app/publish /p:UseAppHost=false

ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 8085
ENTRYPOINT ["dotnet", "Defra.TradeImportsReportingApi.Api.dll"]
