namespace Defra.TradeImportsReportingApi.Api.Data.Entities;

public class Notification
{
    public required string Id { get; init; }
    public required DateTime NotificationCreated { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string ReferenceNumber { get; init; }

    /// <summary>
    /// See NotificationType for values.
    /// </summary>
    public required string NotificationType { get; init; }
}
