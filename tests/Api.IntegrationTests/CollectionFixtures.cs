using Defra.TradeImportsReportingApi.Api.IntegrationTests.Clients;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests;

[CollectionDefinition("UsesWireMockClient")]
public class WireMockClientCollection : ICollectionFixture<WireMockClient>;
