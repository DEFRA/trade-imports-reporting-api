using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public record ReleasesSummaryResponse(
    [property: JsonPropertyName("automatic")] int Automatic,
    [property: JsonPropertyName("manual")] int Manual,
    [property: JsonPropertyName("total")] int Total
);
