using System.Net;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace Defra.TradeImportsReportingApi.Api.Tests.Endpoints;

public class GetSummaryTests(ApiWebApplicationFactory factory, ITestOutputHelper outputHelper)
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

        var response = await client.GetAsync(Testing.Endpoints.Summary.Get());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WhenAuthorized_ShouldBeOk()
    {
        var client = CreateClient();
        var from = new DateTime(2025, 9, 3, 15, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 9, 3, 16, 0, 0, DateTimeKind.Utc);
        MockReportRepository
            .GetReleasesSummary(from, to, Arg.Any<CancellationToken>())
            .Returns(new ReleasesSummary(1, 3, 4));
        MockReportRepository
            .GetMatchesSummary(from, to, Arg.Any<CancellationToken>())
            .Returns(new MatchesSummary(10, 5, 15));
        MockReportRepository
            .GetClearanceRequestsSummary(from, to, Arg.Any<CancellationToken>())
            .Returns(new ClearanceRequestsSummary(10, 20));
        MockReportRepository
            .GetNotificationsSummary(from, to, Arg.Any<CancellationToken>())
            .Returns(new NotificationsSummary(10, 20, 30, 40, 100));

        var response = await client.GetAsync(
            Testing.Endpoints.Summary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }

    [Fact]
    public async Task Get_WhenAuthorized_AndFromAfterTo_ShouldBeBadRequest()
    {
        var client = CreateClient();

        var response = await client.GetAsync(
            Testing.Endpoints.Summary.Get(
                EndpointQuery
                    .New.Where(EndpointFilter.From(DateTime.UtcNow.AddDays(1)))
                    .Where(EndpointFilter.To(DateTime.UtcNow))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().ScrubMember("traceId");
    }

    [Fact]
    public async Task Get_WhenAuthorized_AndDateSpanGreaterThan31Days_ShouldBeBadRequest()
    {
        var client = CreateClient();

        var response = await client.GetAsync(
            Testing.Endpoints.Summary.Get(
                EndpointQuery
                    .New.Where(EndpointFilter.From(DateTime.UtcNow))
                    .Where(EndpointFilter.To(DateTime.UtcNow.AddDays(32)))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().ScrubMember("traceId");
    }

    [Fact]
    public async Task Get_WhenAuthorized_AndFromAndToNotUtc_ShouldBeBadRequest()
    {
        var client = CreateClient();

        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        var response = await client.GetAsync(
            Testing.Endpoints.Summary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(now)).Where(EndpointFilter.To(now.AddDays(1)))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().ScrubMember("traceId");
    }
}
