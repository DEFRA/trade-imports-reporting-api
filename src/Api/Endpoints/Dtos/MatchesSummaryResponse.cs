using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record MatchesSummaryResponse(
    [property: JsonPropertyName("match")] int Match,
    [property: JsonPropertyName("noMatch")] int NoMatch,
    [property: JsonPropertyName("total")] int Total
);
