using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record LastCreatedResponse([property: JsonPropertyName("decision")] LastMessageResponse? Decision);
