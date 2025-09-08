using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public record NotificationsSummaryResponse(
    [property: JsonPropertyName("chedA")] int ChedA,
    [property: JsonPropertyName("chedP")] int ChedP,
    [property: JsonPropertyName("chedPp")] int ChedPp,
    [property: JsonPropertyName("chedD")] int ChedD,
    [property: JsonPropertyName("total")] int Total
);
