using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record ChedReservationsSummaryResponse(
    [property: JsonPropertyName("unreserved")] int Unreserved,
    [property: JsonPropertyName("reserved")] int Reserved,
    [property: JsonPropertyName("consumed")] int Consumed,
    [property: JsonPropertyName("unsuccessful")] int Unsuccessful,
    [property: JsonPropertyName("deleted")] int Deleted,
    [property: JsonPropertyName("total")] int Total
);
