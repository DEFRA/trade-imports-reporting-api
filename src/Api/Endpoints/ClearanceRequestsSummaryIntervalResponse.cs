using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public record ClearanceRequestsSummaryIntervalResponse([property: JsonPropertyName("unique")] int Unique);
