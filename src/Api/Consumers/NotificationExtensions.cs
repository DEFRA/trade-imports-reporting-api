using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Bson;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

public static class NotificationExtensions
{
    private const string ChedA = "CVEDA";
    private const string ChedP = "CVEDP";
    private const string ChedPp = "CHEDPP";
    private const string ChedD = "CED";

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
                ChedA => NotificationType.ChedA,
                ChedP => NotificationType.ChedP,
                ChedPp => NotificationType.ChedPp,
                ChedD => NotificationType.ChedD,
                _ => NotificationType.Unknown,
            },
        };
    }

    public static bool ShouldBeStored(this Notification notification) =>
        notification.NotificationType is not NotificationType.Unknown;
}
