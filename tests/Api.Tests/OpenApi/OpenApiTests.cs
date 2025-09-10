namespace Defra.TradeImportsReportingApi.Api.Tests.OpenApi;

public class OpenApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    [Fact]
    public async Task OpenApi_VerifyAsExpected()
    {
        var client = factory.CreateClient();
        var response = await client.GetStringAsync("/.well-known/openapi/v1/openapi.json");

        await Verify(response);
    }

    [Fact]
    public async Task Redoc_VerifyAsExpected()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/redoc/index.html");

        await Verify(response);
    }
}
