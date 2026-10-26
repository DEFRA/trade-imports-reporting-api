using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Data.Extensions;
using Defra.TradeImportsReportingApi.Api.Models;
using ImportPreNotification = Defra.TradeImportsDataApi.Domain.Ipaffs.ImportPreNotification;

namespace Defra.TradeImportsReportingApi.Api.Tests.Data.Extensions;

public class ImportPreNotificationExtensionsTests
{
    [Fact]
    public async Task ToImportPreNotification_MapAsExpected()
    {
        var @event = new ImportPreNotificationEvent
        {
            Id = "event-id",
            Created = new DateTime(2025, 7, 3, 13, 42, 0, DateTimeKind.Utc),
            Updated = new DateTime(2025, 7, 3, 14, 42, 0, DateTimeKind.Utc),
            ImportPreNotification = new ImportPreNotification
            {
                ImportNotificationType = ImportPreNotificationType.CVEDA,
                Status = "SUBMITTED",
                PartOne = new PartOne
                {
                    Commodities = new Commodities
                    {
                        CommodityComplements =
                        [
                            new CommodityComplements { CommodityId = "0101210000", CommodityDescription = "Horses" },
                            new CommodityComplements { CommodityId = "0102291000", CommodityDescription = "Cattle" },
                        ],
                    },
                },
            },
        };

        var subject = @event.ToImportPreNotification("CHEDA.GB.2025.1234567");

        await Verify(subject).DontScrubDateTimes();
    }

    [Fact]
    public void ToImportPreNotification_WhenNoCommodities_ReturnsEmptyCommodities()
    {
        var @event = new ImportPreNotificationEvent
        {
            Id = "event-id",
            ImportPreNotification = new ImportPreNotification
            {
                ImportNotificationType = ImportPreNotificationType.CVEDP,
            },
        };

        var subject = @event.ToImportPreNotification("ched");

        subject.Id.Should().Be("ched");
        subject.NotificationType.Should().Be(NotificationType.ChedP);
        subject.Status.Should().BeNull();
        subject.Commodities.Should().BeEmpty();
    }
}
