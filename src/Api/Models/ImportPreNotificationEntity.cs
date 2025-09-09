using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsReportingApi.Api.Models;

// ReSharper disable once ClassNeverInstantiated.Global
public class ImportPreNotificationEntity
{
    public DateTime Created { get; init; }
    public DateTime Updated { get; init; }
    public required ImportPreNotification ImportPreNotification { get; init; }
}
