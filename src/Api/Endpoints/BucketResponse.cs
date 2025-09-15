using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public record BucketResponse<T>(
    [property: JsonPropertyName("bucket")] DateTime Bucket,
    [property: JsonPropertyName("summary")] T Summary
);
