using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record ClearanceRequestsSummaryIntervalResponse([property: JsonPropertyName("unique")] int Unique);
