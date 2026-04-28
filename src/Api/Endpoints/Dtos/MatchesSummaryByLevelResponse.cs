using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record MatchesSummaryByLevelResponse(
    [property: JsonPropertyName("level1")] int Level1,
    [property: JsonPropertyName("level2")] int Level2,
    [property: JsonPropertyName("level3")] int Level3,
    [property: JsonPropertyName("total")] int Total
);
