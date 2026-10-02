using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Bson;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

public static class ChedReservationExtensions
{
    public static ChedReservation ToChedReservation(this ResourceEvent<ChedReservationEvent> resourceEvent) =>
        new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Timestamp = resourceEvent.Timestamp,
            ResourceId = resourceEvent.ResourceId,
            Operation = resourceEvent.Operation,
            ChedId = resourceEvent.Resource?.Reservation?.ChedId,
            Mrn = resourceEvent.Resource?.Reservation?.Mrn,
            Status = resourceEvent.Resource?.Reservation?.Status,
        };
}
