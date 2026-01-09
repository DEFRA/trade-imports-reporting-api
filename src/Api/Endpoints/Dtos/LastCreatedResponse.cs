using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

[ExcludeFromCodeCoverage]
public record LastCreatedResponse([property: JsonPropertyName("decision")] LastMessageResponse? Decision);
