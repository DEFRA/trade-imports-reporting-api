using System.Runtime.CompilerServices;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Testing;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.Extracts;

public class ExtractTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    private static readonly DateTime s_updated = new(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
    private static readonly DateTime s_from = s_updated.AddHours(-1);
    private static readonly DateTime s_to = s_updated.AddHours(1);

    private static readonly ImportPreNotificationCommodity[] s_commodities =
    [
        new() { CommodityCode = "0101210000", Description = "Horses" },
        new() { CommodityCode = "0102291000", Description = "Cattle" },
    ];

    [Fact]
    public async Task WhenIpaffsChedsInAndOutOfRange_ShouldReturnOneRowPerCommodityInRange()
    {
        await ImportPreNotifications.InsertManyAsync([
            CreateImportPreNotification("CHEDA.GB.2025.0000001", s_updated),
            CreateImportPreNotification("CHEDA.GB.2025.0000002", s_to.AddMinutes(1)),
        ]);

        await VerifyExtract(Testing.Endpoints.Extracts.IpaffsCheds(Query()));
    }

    [Fact]
    public async Task WhenTracesChedsInAndOutOfRange_ShouldReturnOneRowPerCommodityInRange()
    {
        await TracesCheds.InsertManyAsync([
            CreateTracesChed("CHEDA.GB.2025.0000003", s_updated),
            CreateTracesChed("CHEDA.GB.2025.0000004", s_from.AddMinutes(-1)),
        ]);

        await VerifyExtract(Testing.Endpoints.Extracts.TracesCheds(Query()));
    }

    [Fact]
    public async Task WhenCustomsDeclarationsInAndOutOfRange_ShouldReturnOneRowPerCommodityInRange()
    {
        await CustomsDeclarations.InsertManyAsync([
            CreateCustomsDeclaration("25GB0XX00XXXXX0001", s_updated),
            CreateCustomsDeclaration("25GB0XX00XXXXX0002", s_to.AddMinutes(1)),
        ]);

        await VerifyExtract(Testing.Endpoints.Extracts.CustomsDeclarations(Query()));
    }

    private async Task VerifyExtract(string url, [CallerMemberName] string testName = "")
    {
        var response = await DefaultClient.GetAsync(url);
        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{testName}_json");

        var csvRequest = new HttpRequestMessage(HttpMethod.Get, url);
        csvRequest.Headers.Add("Accept", "text/csv");
        response = await DefaultClient.SendAsync(csvRequest);
        await Verify(await response.Content.ReadAsStringAsync()).DontScrubDateTimes().UseMethodName($"{testName}_csv");
    }

    private static EndpointQuery Query() =>
        EndpointQuery.New.Where(EndpointFilter.From(s_from)).Where(EndpointFilter.To(s_to));

    private static ImportPreNotification CreateImportPreNotification(string id, DateTime updated) =>
        new()
        {
            Id = id,
            Created = updated.AddMinutes(-10),
            Updated = updated,
            NotificationType = NotificationType.ChedA,
            Status = "SUBMITTED",
            Commodities = s_commodities,
        };

    private static TracesChed CreateTracesChed(string id, DateTime updated) =>
        new()
        {
            Id = id,
            Created = updated.AddMinutes(-10),
            Updated = updated,
            NotificationType = NotificationType.ChedA,
            Status = "VALIDATED",
            Commodities = s_commodities,
        };

    private static CustomsDeclaration CreateCustomsDeclaration(string mrn, DateTime lastUpdated) =>
        new()
        {
            Id = mrn,
            MrnCreated = lastUpdated.AddMinutes(-10),
            Timestamp = lastUpdated.AddMinutes(-10),
            LastUpdated = lastUpdated,
            ReleaseType = ReleaseType.Automatic,
            FinalState = "0",
            DeclarantId = "GB123456789000",
            DispatchCountryCode = "IT",
            Items =
            [
                new CustomsDeclarationItem
                {
                    Number = 1,
                    CommodityCode = "0101210000",
                    Description = "Horses",
                    QuantityOrWeight = 175,
                    ChedReference = "GBCHD2025.0000001",
                    Authority = "AHVLA",
                    CheckCode = "H221",
                    Decision = "C03",
                    Match = true,
                },
                new CustomsDeclarationItem
                {
                    Number = 2,
                    CommodityCode = "0102291000",
                    Description = "Cattle",
                    QuantityOrWeight = 50,
                    ChedReference = "GBCHD2025.0000002",
                    Authority = "AHVLA",
                    CheckCode = "H221",
                    Decision = "X00",
                    Match = false,
                },
            ],
        };
}
