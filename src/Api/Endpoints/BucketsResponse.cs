using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public record BucketsResponse<T>([property: JsonPropertyName("buckets")] IReadOnlyList<T> Buckets);
