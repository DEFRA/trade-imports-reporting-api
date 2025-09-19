using System.Net;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Endpoints;
using Defra.TradeImportsReportingApi.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace Defra.TradeImportsReportingApi.Api.Tests.Endpoints.General;

public class GetBucketsTests(ApiWebApplicationFactory factory, ITestOutputHelper outputHelper)
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

        var response = await client.GetAsync(Testing.Endpoints.Buckets.Get());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WhenAuthorized_ShouldBeOk()
    {
        var client = CreateClient();
        var from = new DateTime(2025, 9, 3, 15, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 9, 3, 16, 0, 0, DateTimeKind.Utc);
        MockReportRepository
            .GetReleasesBuckets(from, to, Units.Hour, Arg.Any<CancellationToken>())
            .Returns(
                [
                    new ReleasesBucket(from, new ReleasesSummary(1, 3, 4)),
                    new ReleasesBucket(to, new ReleasesSummary(2, 4, 5)),
                ]
            );
        MockReportRepository
            .GetMatchesBuckets(from, to, Units.Hour, Arg.Any<CancellationToken>())
            .Returns(
                [
                    new MatchesBucket(from, new MatchesSummary(1, 3, 4)),
                    new MatchesBucket(to, new MatchesSummary(2, 4, 5)),
                ]
            );
        MockReportRepository
            .GetClearanceRequestsBuckets(from, to, Units.Hour, Arg.Any<CancellationToken>())
            .Returns(
                [
                    new ClearanceRequestsBucket(from, new ClearanceRequestsSummary(1, -1)),
                    new ClearanceRequestsBucket(to, new ClearanceRequestsSummary(2, -1)),
                ]
            );
        MockReportRepository
            .GetNotificationsBuckets(from, to, Units.Hour, Arg.Any<CancellationToken>())
            .Returns(
                [
                    new NotificationsBucket(from, new NotificationsSummary(10, 20, 30, 40, 100)),
                    new NotificationsBucket(to, new NotificationsSummary(1, 2, 3, 4, 10)),
                ]
            );

        var response = await client.GetAsync(
            Testing.Endpoints.Buckets.Get(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
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
            Testing.Endpoints.Buckets.Get(
                EndpointQuery
                    .New.Where(EndpointFilter.From(DateTime.UtcNow.AddDays(1)))
                    .Where(EndpointFilter.To(DateTime.UtcNow))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().ScrubMember("traceId");
    }

    [Fact]
    public async Task Get_WhenAuthorized_AndDateSpanGreaterThanAllowedDays_ShouldBeBadRequest()
    {
        var client = CreateClient();

        var response = await client.GetAsync(
            Testing.Endpoints.Buckets.Get(
                EndpointQuery
                    .New.Where(EndpointFilter.From(DateTime.UtcNow))
                    .Where(EndpointFilter.To(DateTime.UtcNow.AddDays(TimePeriod.MaxDays + 1)))
                    .Where(EndpointFilter.Unit(Units.Hour))
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
            Testing.Endpoints.Buckets.Get(
                EndpointQuery
                    .New.Where(EndpointFilter.From(now))
                    .Where(EndpointFilter.To(now.AddDays(1)))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().ScrubMember("traceId");
    }

    [Fact]
    public async Task Get_WhenAuthorized_AndUnitUnknown_ShouldBeBadRequest()
    {
        var client = CreateClient();

        var now = DateTime.UtcNow;
        var response = await client.GetAsync(
            Testing.Endpoints.Buckets.Get(
                EndpointQuery
                    .New.Where(EndpointFilter.From(now))
                    .Where(EndpointFilter.To(now.AddDays(1)))
                    .Where(EndpointFilter.Unit("unknown"))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().ScrubMember("traceId");
    }
}
