using Defra.TradeImportsReportingApi.Api.IntegrationTests.Clients;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.TestBase;

[CollectionDefinition("UsesWireMockClient")]
public class WireMockClientCollection : ICollectionFixture<WireMockClient>;
