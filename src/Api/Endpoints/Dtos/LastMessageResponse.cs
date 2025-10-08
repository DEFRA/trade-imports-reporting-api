using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record LastMessageResponse(
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("reference")] string Reference
);
