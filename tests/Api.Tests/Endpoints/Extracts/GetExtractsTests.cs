using System.Net;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Endpoints;
using Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;
using Defra.TradeImportsReportingApi.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace Defra.TradeImportsReportingApi.Api.Tests.Endpoints.Extracts;

public class GetExtractsTests(ApiWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestBase(factory, outputHelper)
{
    private static readonly DateTime s_from = new(2025, 9, 3, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime s_to = new(2025, 9, 3, 16, 0, 0, DateTimeKind.Utc);

    public static TheoryData<string> Extracts => ["ipaffs-cheds", "traces-cheds", "customs-declarations"];

    private IExtractRepository MockExtractRepository { get; } = Substitute.For<IExtractRepository>();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        services.AddTransient<IExtractRepository>(_ => MockExtractRepository);
    }

    [Theory]
    [MemberData(nameof(Extracts))]
    public async Task Get_WhenUnauthorized_ShouldBeUnauthorized(string extract)
    {
        var client = CreateClient(addDefaultAuthorizationHeader: false);

        var response = await client.GetAsync(Url(extract, s_from, s_to));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(Extracts))]
    public async Task Get_WhenAuthorized_ShouldBeOk(string extract)
    {
        SetupRepository();
        var client = CreateClient();

        var response = await client.GetAsync(Url(extract, s_from, s_to));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .DontScrubDateTimes()
            .UseParameters(extract);
    }

    [Theory]
    [MemberData(nameof(Extracts))]
    public async Task Get_WhenAuthorized_AndRequestingCsv_ShouldBeOk(string extract)
    {
        SetupRepository();
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Accept", "text/csv");

        var response = await client.GetAsync(Url(extract, s_from, s_to));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");

        await Verify(await response.Content.ReadAsStringAsync()).DontScrubDateTimes().UseParameters(extract);
    }

    [Theory]
    [MemberData(nameof(Extracts))]
    public async Task Get_WhenAuthorized_AndFromAfterTo_ShouldBeBadRequest(string extract)
    {
        var client = CreateClient();

        var response = await client.GetAsync(Url(extract, s_to, s_from));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .ScrubMember("traceId")
            .UseParameters(extract);
    }

    [Theory]
    [MemberData(nameof(Extracts))]
    public async Task Get_WhenAuthorized_AndDateSpanGreaterThanAllowedDays_ShouldBeBadRequest(string extract)
    {
        var client = CreateClient();

        var response = await client.GetAsync(Url(extract, s_from, s_from.AddDays(TimePeriod.MaxDays + 1)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .ScrubMember("traceId")
            .UseParameters(extract);
    }

    [Theory]
    [MemberData(nameof(Extracts))]
    public async Task Get_WhenAuthorized_AndFromAndToNotUtc_ShouldBeBadRequest(string extract)
    {
        var client = CreateClient();
        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        var response = await client.GetAsync(Url(extract, now, now.AddDays(1)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .ScrubMember("traceId")
            .UseParameters(extract);
    }

    private static string Url(string extract, DateTime from, DateTime to)
    {
        var query = EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to));

        return extract switch
        {
            "ipaffs-cheds" => Testing.Endpoints.Extracts.IpaffsCheds(query),
            "traces-cheds" => Testing.Endpoints.Extracts.TracesCheds(query),
            "customs-declarations" => Testing.Endpoints.Extracts.CustomsDeclarations(query),
            _ => throw new ArgumentOutOfRangeException(nameof(extract), extract, null),
        };
    }

    private void SetupRepository()
    {
        IReadOnlyList<ChedExtractResponse> cheds =
        [
            new()
            {
                ChedReference = "CHEDA.GB.2025.1234567",
                ChedType = "ChedA",
                Status = "SUBMITTED",
                Created = new DateTime(2025, 9, 3, 15, 10, 0, DateTimeKind.Utc),
                Updated = new DateTime(2025, 9, 3, 15, 20, 0, DateTimeKind.Utc),
                CommodityCode = "0101210000",
                Description = "Horses, \"live\"",
            },
            new()
            {
                ChedReference = "CHEDA.GB.2025.1234567",
                ChedType = "ChedA",
                Status = "SUBMITTED",
                Created = new DateTime(2025, 9, 3, 15, 10, 0, DateTimeKind.Utc),
                Updated = new DateTime(2025, 9, 3, 15, 20, 0, DateTimeKind.Utc),
                CommodityCode = "0102291000",
                Description = "Cattle",
            },
        ];

        MockExtractRepository.GetIpaffsCheds(s_from, s_to, Arg.Any<CancellationToken>()).Returns(cheds);
        MockExtractRepository.GetTracesCheds(s_from, s_to, Arg.Any<CancellationToken>()).Returns(cheds);
        MockExtractRepository
            .GetCustomsDeclarations(s_from, s_to, Arg.Any<CancellationToken>())
            .Returns(
                [
                    new CustomsDeclarationExtractResponse
                    {
                        Mrn = "25GB0XX00XXXXX0000",
                        MrnCreated = new DateTime(2025, 9, 3, 15, 10, 0, DateTimeKind.Utc),
                        LastUpdated = new DateTime(2025, 9, 3, 15, 20, 0, DateTimeKind.Utc),
                        ReleaseType = "Automatic",
                        FinalState = "0",
                        DispatchCountryCode = "IT",
                        ItemNumber = 1,
                        CommodityCode = "0101210000",
                        Description = "Horses",
                        QuantityOrWeight = 175,
                        ChedReferences = "GBCHD2025.1; GBCHD2025.2",
                        Authorities = "AHVLA",
                        CheckCodes = "H221",
                        Decisions = "C03; X00",
                        Match = "No",
                    },
                ]
            );
    }
}
