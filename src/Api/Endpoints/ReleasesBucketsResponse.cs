using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public record ReleasesBucketsResponse(
    [property: JsonPropertyName("buckets")] IReadOnlyList<ReleasesBucketResponse> Buckets
);
