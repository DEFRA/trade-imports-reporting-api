using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record LastSentResponse([property: JsonPropertyName("decision")] LastMessageResponse? Decision);
