using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Models;
using MongoDB.Bson;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

public static class NotificationExtensions
{
    public static Notification ToNotification(
        this TradeImportsDataApi.Domain.Ipaffs.ImportPreNotification notification,
        string referenceNumber,
        DateTime notificationCreated,
        DateTime notificationUpdated
    )
    {
        return new Notification
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Timestamp = notificationUpdated,
            ReferenceNumber = referenceNumber,
            NotificationCreated = notificationCreated,
            NotificationType = notification.ImportNotificationType switch
            {
                ImportPreNotificationType.CVEDA => NotificationType.ChedA,
                ImportPreNotificationType.CVEDP => NotificationType.ChedP,
                ImportPreNotificationType.CHEDPP => NotificationType.ChedPP,
                ImportPreNotificationType.CED => NotificationType.ChedD,
                _ => NotificationType.Unknown,
            },
        };
    }

    public static bool ShouldBeStored(this Notification notification) =>
        notification.NotificationType is not NotificationType.Unknown;
}
