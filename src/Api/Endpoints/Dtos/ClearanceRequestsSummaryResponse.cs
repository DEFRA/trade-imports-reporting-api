using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record ClearanceRequestsSummaryResponse(
    [property: JsonPropertyName("unique")] int Unique,
    [property: JsonPropertyName("total")] int Total
);
