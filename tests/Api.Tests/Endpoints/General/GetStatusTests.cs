using System.Net;
using Defra.TradeImportsReportingApi.Api.Data;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace Defra.TradeImportsReportingApi.Api.Tests.Endpoints.General;

public class GetStatusTests(ApiWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestBase(factory, outputHelper)
{
    private IReportRepository MockReportRepository { get; } = Substitute.For<IReportRepository>();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        services.AddTransient<IReportRepository>(_ => MockReportRepository);
    }

    [Fact]
    public async Task Get_WhenUnauthorized_ShouldBeUnauthorized()
    {
        var client = CreateClient(addDefaultAuthorizationHeader: false);

        var response = await client.GetAsync(Testing.Endpoints.Status.Get());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WhenAuthorized_ShouldBeOk()
    {
        var client = CreateClient();
        MockReportRepository
            .GetLastReceivedSummary(Arg.Any<CancellationToken>())
            .Returns(
                new LastReceivedSummary(
                    new LastReceived(new DateTime(2025, 9, 8, 16, 0, 0, DateTimeKind.Utc), "mrn1"),
                    new LastReceived(new DateTime(2025, 9, 8, 17, 0, 0, DateTimeKind.Utc), "mrn2"),
                    new LastReceived(new DateTime(2025, 9, 8, 18, 0, 0, DateTimeKind.Utc), "ched")
                )
            );
        MockReportRepository
            .GetLastSentSummary(Arg.Any<CancellationToken>())
            .Returns(new LastSentSummary(new LastSent(new DateTime(2025, 9, 8, 19, 0, 0, DateTimeKind.Utc), "mrn3")));

        MockReportRepository
            .GetLastCreatedSummary(Arg.Any<CancellationToken>())
            .Returns(
                new LastCreatedSummary(new LastCreated(new DateTime(2025, 9, 8, 19, 0, 0, DateTimeKind.Utc), "mrn3"))
            );

        var response = await client.GetAsync(Testing.Endpoints.Status.Get());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }
}
