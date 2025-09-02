using RestEase;
using WireMock.Client;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Clients;

public class WireMockClient
{
    public WireMockClient()
    {
        WireMockAdminApi.ResetMappingsAsync().GetAwaiter().GetResult();
        WireMockAdminApi.ResetRequestsAsync().GetAwaiter().GetResult();
    }

    public IWireMockAdminApi WireMockAdminApi { get; } = RestClient.For<IWireMockAdminApi>("http://localhost:9090");
}
