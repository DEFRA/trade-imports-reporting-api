using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public record ReleasesResponse(
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("reference")] string Reference
);
