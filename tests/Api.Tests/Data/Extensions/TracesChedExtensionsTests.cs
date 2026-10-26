using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Data.Extensions;
using Defra.TradeImportsReportingApi.Api.Models;
using Trade.Gateway.Api.Contract.Certificate;
using ImportPreNotification = Defra.TradeImportsReportingApi.Api.Data.Entities.ImportPreNotification;

namespace Defra.TradeImportsReportingApi.Api.Tests.Data.Extensions;

public class TracesChedExtensionsTests
{
    [Fact]
    public async Task ToTracesChed_MapAsExpected()
    {
        var @event = CreateEvent(
            "CHEDA.GB.2025.1234567",
            TracesNotificationStatus.Submitted,
            ("0101210000", "Horses"),
            ("0102291000", "Cattle")
        );

        var subject = @event.ToTracesChed("CHEDA.GB.2025.1234567");

        await Verify(subject).DontScrubDateTimes();
    }

    [Theory]
    [InlineData("CHEDA.GB.2025.1234567", NotificationType.ChedA)]
    [InlineData("CHEDP.GB.2025.1234567", NotificationType.ChedP)]
    [InlineData("CHEDPP.GB.2025.1234567", NotificationType.ChedPP)]
    [InlineData("CHEDD.GB.2025.1234567", NotificationType.ChedD)]
    [InlineData("INTRA.GB.2025.1234567", NotificationType.Unknown)]
    public void ToTracesChed_NotificationType_ShouldBeAsExpected(string identifier, string expected)
    {
        CreateEvent(identifier, null).ToTracesChed(identifier).NotificationType.Should().Be(expected);
    }

    [Theory]
    [InlineData(TracesNotificationStatus.Submitted, ImportNotificationStatus.Submitted)]
    [InlineData(TracesNotificationStatus.Validated, ImportNotificationStatus.Validated)]
    [InlineData(TracesNotificationStatus.InProgress, ImportNotificationStatus.InProgress)]
    [InlineData(TracesNotificationStatus.Replaced, ImportNotificationStatus.Replaced)]
    [InlineData(TracesNotificationStatus.Cancelled, ImportNotificationStatus.Cancelled)]
    [InlineData(TracesNotificationStatus.Deleted, ImportNotificationStatus.Deleted)]
    [InlineData(TracesNotificationStatus.SplitConsignment, ImportNotificationStatus.SplitConsignment)]
    [InlineData("999", "999")]
    [InlineData(null, null)]
    public void ToTracesChed_Status_ShouldBeAsExpected(string? documentStatusCode, string? expected)
    {
        CreateEvent("CHEDA.GB.2025.1234567", documentStatusCode)
            .ToTracesChed("ched")
            .Status.Should()
            .Be(expected);
    }

    [Fact]
    public void ToTracesChed_WhenNoConsignmentItems_ReturnsEmptyCommodities()
    {
        CreateEvent("CHEDA.GB.2025.1234567", null).ToTracesChed("ched").Commodities.Should().BeEmpty();
    }

    [Fact]
    public void TracesChed_HasIdenticalFieldsToImportPreNotification()
    {
        static IEnumerable<(string, Type)> Fields(Type type) =>
            type.GetProperties().Select(x => (x.Name, x.PropertyType)).OrderBy(x => x.Name);

        Fields(typeof(TracesChed)).Should().Equal(Fields(typeof(ImportPreNotification)));
    }

    private static TracesChedEvent CreateEvent(
        string identifier,
        string? documentStatusCode,
        params (string Code, string Description)[] commodities
    ) =>
        new()
        {
            Id = identifier,
            Created = new DateTime(2025, 7, 3, 13, 42, 0, DateTimeKind.Utc),
            Updated = new DateTime(2025, 7, 3, 14, 42, 0, DateTimeKind.Utc),
            Ched = new DefraUNVTDCHEDProfile
            {
                ExchangedDocument = new ExchangedDocument
                {
                    Identifier = identifier,
                    DocumentStatusCode = documentStatusCode,
                },
                SpecifiedConsignment = new Consignment
                {
                    IncludedConsignmentItem =
                    [
                        new ConsignmentItem
                        {
                            IncludedTradeLineItem =
                            [
                                .. commodities.Select(x => new TradeLineItem
                                {
                                    ApplicableClassification =
                                    [
                                        new ApplicableClassification
                                        {
                                            ClassCode = new CodedValue { Value = x.Code },
                                            ClassName = [x.Description],
                                        },
                                    ],
                                }),
                            ],
                        },
                    ],
                },
            },
        };
}
