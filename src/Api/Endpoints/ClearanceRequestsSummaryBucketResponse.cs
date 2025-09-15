using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public record ClearanceRequestsSummaryBucketResponse([property: JsonPropertyName("unique")] int Unique);
