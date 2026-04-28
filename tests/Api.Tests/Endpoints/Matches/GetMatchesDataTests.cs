using System.Net;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Endpoints;
using Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;
using Defra.TradeImportsReportingApi.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace Defra.TradeImportsReportingApi.Api.Tests.Endpoints.Matches;

public class GetMatchesDataTests(ApiWebApplicationFactory factory, ITestOutputHelper outputHelper)
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

        var response = await client.GetAsync(Testing.Endpoints.Matches.Data());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WhenAuthorized_ShouldBeOk()
    {
        var client = CreateClient();
        var from = new DateTime(2025, 9, 3, 15, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 9, 3, 16, 0, 0, DateTimeKind.Utc);
        const bool match = true;
        MockReportRepository
            .GetMatchesV2(from, to, match, null, Arg.Any<CancellationToken>())
            .Returns(
                [
                    new MatchResponseV2()
                    {
                        Number = 1,
                        Timestamp = new DateTime(2025, 9, 15, 16, 31, 5, DateTimeKind.Utc),
                        Mrn = "mrn1",
                        ChedReference = "chedReference1",
                        CheckCode = "H222",
                        Authority = "authority1",
                        Match = "Yes",
                        CommodityCode = "commodityCode1",
                        Decision = "X00",
                        Description = "description1",
                    },
                    new MatchResponseV2()
                    {
                        Number = 2,
                        Timestamp = new DateTime(2025, 9, 15, 16, 41, 5, DateTimeKind.Utc),
                        Mrn = "mrn2",
                        ChedReference = "chedReference1",
                        CheckCode = "H222",
                        Authority = "authority2",
                        Match = "Yes",
                        CommodityCode = "commodityCode2",
                        Decision = "X00",
                        Description = "description2",
                    },
                ]
            );

        client.DefaultRequestHeaders.Add("UseV2", "true");
        var response = await client.GetAsync(
            Testing.Endpoints.Matches.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Match(match))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }

    [Theory]
    [InlineData("mrn", true, null)]
    [InlineData("mrn'1", true, null)]
    [InlineData("mrn\"1", true, null)]
    [InlineData("mrn", true, 1)]
    [InlineData("mrn", true, 2)]
    [InlineData("mrn", true, 3)]
    [InlineData("mrn", false, null)]
    public async Task Get_WhenAuthorized_AndRequestingCsv_ShouldBeOk(string mrn, bool match, int? matchLevel)
    {
        var client = CreateClient();
        var from = new DateTime(2025, 9, 3, 15, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 9, 3, 16, 0, 0, DateTimeKind.Utc);

        MockReportRepository
            .GetMatchesV2(from, to, match, matchLevel, Arg.Any<CancellationToken>())
            .Returns(
                [
                    new MatchResponseV2()
                    {
                        Number = 1,
                        Timestamp = new DateTime(2025, 9, 15, 16, 31, 5, DateTimeKind.Utc),
                        Mrn = "mrn",
                        ChedReference = "chedReference1",
                        CheckCode = "H222",
                        Authority = "authority1",
                        Match = "Yes",
                        CommodityCode = "commodityCode1",
                        Decision = "X00",
                        Description = "description1",
                        Level = null,
                    },
                    new MatchResponseV2()
                    {
                        Number = 2,
                        Timestamp = new DateTime(2025, 9, 15, 16, 41, 5, DateTimeKind.Utc),
                        Mrn = "mrn2",
                        ChedReference = "chedReference1",
                        CheckCode = "H222",
                        Authority = "authority2",
                        Match = "Yes",
                        CommodityCode = "commodityCode2",
                        Decision = "X00",
                        Description = "description2",
                        Level = null,
                    },
                ]
            );

        client.DefaultRequestHeaders.Add("Accept", "text/csv");
        client.DefaultRequestHeaders.Add("UseV2", "true");

        var endpointQuery = EndpointQuery
            .New.Where(EndpointFilter.From(from))
            .Where(EndpointFilter.To(to))
            .Where(EndpointFilter.Match(match));

        if (matchLevel != null)
        {
            endpointQuery = endpointQuery.Where(EndpointFilter.MatchLevel(matchLevel.Value));
        }

        var response = await client.GetAsync(Testing.Endpoints.Matches.Data(endpointQuery));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await Verify(await response.Content.ReadAsStringAsync())
            .UseParameters(mrn, match, matchLevel)
            .DontScrubDateTimes();
    }

    [Theory]
    [InlineData("mrn1")]
    [InlineData("mrn'1")]
    [InlineData("mrn\"1")]
    public async Task Get_WhenAuthorized_AndRequestingCsvV1_ShouldBeOk(string mrn1)
    {
        var client = CreateClient();
        var from = new DateTime(2025, 9, 3, 15, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 9, 3, 16, 0, 0, DateTimeKind.Utc);
        const bool match = true;
        MockReportRepository
            .GetMatches(from, to, match, Arg.Any<CancellationToken>())
            .Returns(
                [
                    new Decision()
                    {
                        Id = "id1",
                        MrnCreated = default,
                        Timestamp = new DateTime(2025, 9, 15, 16, 31, 5, DateTimeKind.Utc),
                        Mrn = "mrn1",
                        Match = true,
                    },
                    new Decision()
                    {
                        Id = "id1",
                        MrnCreated = default,
                        Timestamp = new DateTime(2025, 9, 15, 16, 41, 5, DateTimeKind.Utc),
                        Mrn = "mrn2",
                        Match = true,
                    },
                ]
            );

        client.DefaultRequestHeaders.Add("Accept", "text/csv");
        var response = await client.GetAsync(
            Testing.Endpoints.Matches.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Match(match))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await Verify(await response.Content.ReadAsStringAsync()).UseParameters(mrn1).DontScrubDateTimes();
    }

    [Fact]
    public async Task Get_WhenAuthorized_AndFromAfterTo_ShouldBeBadRequest()
    {
        var client = CreateClient();

        var response = await client.GetAsync(
            Testing.Endpoints.Matches.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(DateTime.UtcNow.AddDays(1)))
                    .Where(EndpointFilter.To(DateTime.UtcNow))
                    .Where(EndpointFilter.Match(true))
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
            Testing.Endpoints.Matches.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(DateTime.UtcNow))
                    .Where(EndpointFilter.To(DateTime.UtcNow.AddDays(TimePeriod.MaxDays + 1)))
                    .Where(EndpointFilter.Match(true))
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
            Testing.Endpoints.Matches.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(now))
                    .Where(EndpointFilter.To(now.AddDays(1)))
                    .Where(EndpointFilter.Match(true))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().ScrubMember("traceId");
    }

    [Fact]
    public async Task Get_WhenAuthorized_AndMatchAndMatchLevelIncompatible_ShouldBeBadRequest()
    {
        var client = CreateClient();

        var now = DateTime.UtcNow;
        var response = await client.GetAsync(
            Testing.Endpoints.Matches.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(now))
                    .Where(EndpointFilter.To(now.AddDays(1)))
                    .Where(EndpointFilter.Match(false))
                    .Where(EndpointFilter.MatchLevel(1))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().ScrubMember("traceId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4)]
    public async Task Get_WhenAuthorized_AndAndMatchLevelOutOfRange_ShouldBeBadRequest(int matchLevel)
    {
        var client = CreateClient();

        var now = DateTime.UtcNow;
        var response = await client.GetAsync(
            Testing.Endpoints.Matches.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(now))
                    .Where(EndpointFilter.To(now.AddDays(1)))
                    .Where(EndpointFilter.Match(true))
                    .Where(EndpointFilter.MatchLevel(matchLevel))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseParameters(matchLevel)
            .UseStrictJson()
            .ScrubMember("traceId");
    }

    [Fact]
    public async Task Get_WhenAuthorized_AndMatchNotSpecified_ShouldBeBadRequest()
    {
        var client = CreateClient();

        var now = DateTime.UtcNow;
        var response = await client.GetAsync(
            Testing.Endpoints.Matches.Data(
                EndpointQuery.New.Where(EndpointFilter.From(now)).Where(EndpointFilter.To(now.AddDays(1)))
            )
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().ScrubMember("traceId");
    }
}
