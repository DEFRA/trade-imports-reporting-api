using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Consumers;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using ImportPreNotification = Defra.TradeImportsReportingApi.Api.Data.Entities.ImportPreNotification;

namespace Defra.TradeImportsReportingApi.Api.Data.Extensions;

public static class ImportPreNotificationExtensions
{
    public static ImportPreNotification ToImportPreNotification(
        this ImportPreNotificationEvent importPreNotificationEvent,
        string referenceNumber
    )
    {
        var notification = importPreNotificationEvent.ImportPreNotification;

        return new ImportPreNotification
        {
            Id = referenceNumber,
            Created = importPreNotificationEvent.Created,
            Updated = importPreNotificationEvent.Updated,
            NotificationType = notification.ImportNotificationType.ToNotificationType(),
            Status = notification.Status,
            Commodities =
                notification
                    .PartOne?.Commodities?.CommodityComplements?.Select(x => new ImportPreNotificationCommodity
                    {
                        CommodityCode = x.CommodityId,
                        Description = x.CommodityDescription,
                    })
                    .ToArray() ?? [],
        };
    }
}
