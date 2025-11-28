using System.Net.Http.Headers;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Endpoints.Admin;

public class AdminTestBase : SqsTestBase
{
    protected static HttpClient CreateHttpClient(bool withAuthentication = true)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };

        if (withAuthentication)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                // See compose.yml for username, password and scope configuration
                Convert.ToBase64String("IntegrationTests:integration-tests-pwd"u8.ToArray())
            );
        }

        return httpClient;
    }
}
