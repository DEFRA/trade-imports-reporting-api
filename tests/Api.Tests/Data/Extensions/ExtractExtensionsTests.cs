using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Data.Extensions;
using Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

namespace Defra.TradeImportsReportingApi.Api.Tests.Data.Extensions;

public class ExtractExtensionsTests
{
    private static readonly DateTime s_created = new(2025, 7, 3, 13, 42, 0, DateTimeKind.Utc);
    private static readonly DateTime s_updated = new(2025, 7, 3, 14, 42, 0, DateTimeKind.Utc);

    private static readonly ImportPreNotificationCommodity[] s_commodities =
    [
        new() { CommodityCode = "0101210000", Description = "Horses" },
        new() { CommodityCode = "0102291000", Description = "Cattle" },
    ];

    [Fact]
    public async Task ImportPreNotification_ToExtractRows_OneRowPerCommodity()
    {
        var notification = new ImportPreNotification
        {
            Id = "CHEDA.GB.2025.1234567",
            Created = s_created,
            Updated = s_updated,
            NotificationType = NotificationType.ChedA,
            Status = "SUBMITTED",
            Commodities = s_commodities,
        };

        await Verify(notification.ToExtractRows()).DontScrubDateTimes();
    }

    [Fact]
    public void TracesChed_ToExtractRows_MatchesImportPreNotificationRows()
    {
        var tracesChed = new TracesChed
        {
            Id = "CHEDA.GB.2025.1234567",
            Created = s_created,
            Updated = s_updated,
            NotificationType = NotificationType.ChedA,
            Status = "SUBMITTED",
            Commodities = s_commodities,
        };
        var notification = new ImportPreNotification
        {
            Id = tracesChed.Id,
            Created = tracesChed.Created,
            Updated = tracesChed.Updated,
            NotificationType = tracesChed.NotificationType,
            Status = tracesChed.Status,
            Commodities = tracesChed.Commodities,
        };

        tracesChed.ToExtractRows().Should().BeEquivalentTo(notification.ToExtractRows());
    }

    [Fact]
    public void Ched_ToExtractRows_WhenNoCommodities_ReturnsNoRows()
    {
        var tracesChed = new TracesChed
        {
            Id = "CHEDA.GB.2025.1234567",
            Created = s_created,
            Updated = s_updated,
            NotificationType = NotificationType.ChedA,
        };

        tracesChed.ToExtractRows().Should().BeEmpty();
    }

    [Fact]
    public async Task CustomsDeclaration_ToExtractRows_OneRowPerCommodity()
    {
        var customsDeclaration = new CustomsDeclaration
        {
            Id = "25GB0XX00XXXXX0000",
            MrnCreated = s_created,
            Timestamp = s_created,
            LastUpdated = s_updated,
            ReleaseType = ReleaseType.Automatic,
            FinalState = "0",
            DeclarantId = "GB123456789000",
            DispatchCountryCode = "IT",
            Items =
            [
                // Item 1 has two CHEDs, each with its own decision result
                CreateItem(1, "GBCHD2025.1", "C03", match: true),
                CreateItem(1, "GBCHD2025.2", "X00", match: false),
                // Item 2 has one CHED with two decision results
                CreateItem(2, "GBCHD2025.3", "C03", match: true),
                CreateItem(2, "GBCHD2025.3", "C03", match: true),
            ],
        };

        var rows = customsDeclaration.ToExtractRows().ToList();

        rows.Should().HaveCount(2);
        rows[0].Match.Should().Be("No");
        rows[0].ChedReferences.Should().Be("GBCHD2025.1; GBCHD2025.2");
        rows[1].Match.Should().Be("Yes");
        rows[1].ChedReferences.Should().Be("GBCHD2025.3");

        await Verify(rows).DontScrubDateTimes();
    }

    [Fact]
    public void CustomsDeclarationExtractResponse_DoesNotExposeDeclarant()
    {
        typeof(CustomsDeclarationExtractResponse)
            .GetProperties()
            .Select(x => x.Name)
            .Should()
            .NotContain(x => x.Contains("Declarant", StringComparison.OrdinalIgnoreCase));
    }

    private static CustomsDeclarationItem CreateItem(int number, string chedReference, string decision, bool match) =>
        new()
        {
            Number = number,
            CommodityCode = $"010{number}",
            Description = $"Commodity {number}",
            QuantityOrWeight = 10 * number,
            ChedReference = chedReference,
            Authority = "AHVLA",
            CheckCode = "H221",
            Decision = decision,
            Match = match,
        };
}
