using System.Net;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Endpoints;
using Defra.TradeImportsReportingApi.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace Defra.TradeImportsReportingApi.Api.Tests.Endpoints.Notifications;

public class GetNotificationsIntervalsTests(ApiWebApplicationFactory factory, ITestOutputHelper outputHelper)
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

        var response = await client.GetAsync(Testing.Endpoints.Notifications.Intervals());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WhenAuthorized_ShouldBeOk()
    {
        var client = CreateClient();
        var from = new DateTime(2025, 9, 3, 15, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 9, 3, 16, 0, 0, DateTimeKind.Utc);
        var intervals = new[]
        {
            new DateTime(2025, 9, 3, 15, 15, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 3, 15, 30, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 3, 15, 45, 0, DateTimeKind.Utc),
        };
        MockReportRepository
            .GetNotificationsIntervals(
                from,
                to,
                Arg.Is<DateTime[]>(x => x.SequenceEqual(intervals)),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                [
                    new NotificationsBucket(from, new NotificationsSummary(10, 20, 30, 40, 100)),
                    new NotificationsBucket(to, new NotificationsSummary(1, 2, 3, 4, 10)),
                ]
            );

        var response = await client.GetAsync(
            Testing.Endpoints.Notifications.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals(intervals))
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
            Testing.Endpoints.Notifications.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(DateTime.UtcNow.AddDays(1)))
                    .Where(EndpointFilter.To(DateTime.UtcNow))
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
            Testing.Endpoints.Notifications.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(DateTime.UtcNow))
                    .Where(EndpointFilter.To(DateTime.UtcNow.AddDays(TimePeriod.MaxDays + 1)))
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
            Testing.Endpoints.Notifications.Intervals(
                EndpointQuery.New.Where(EndpointFilter.From(now)).Where(EndpointFilter.To(now.AddDays(1)))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().ScrubMember("traceId");
    }

    [Fact]
    public async Task Get_WhenAuthorized_AndOneIntervalNotUtc_ShouldBeBadRequest()
    {
        var client = CreateClient();

        var now = DateTime.UtcNow;
        var response = await client.GetAsync(
            Testing.Endpoints.Notifications.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(now))
                    .Where(EndpointFilter.To(now.AddDays(1)))
                    .Where(EndpointFilter.Intervals([DateTime.SpecifyKind(now, DateTimeKind.Unspecified)]))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().ScrubMember("traceId");
    }
}
