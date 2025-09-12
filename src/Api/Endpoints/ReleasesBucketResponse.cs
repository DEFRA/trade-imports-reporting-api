using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public record ReleasesBucketResponse(
    [property: JsonPropertyName("bucket")] DateTime Bucket,
    [property: JsonPropertyName("summary")] ReleasesSummaryResponse Summary
);
