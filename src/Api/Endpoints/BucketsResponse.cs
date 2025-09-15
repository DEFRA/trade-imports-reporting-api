using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public record BucketsResponse(
    [property: JsonPropertyName("releases")] BucketsResponse<BucketResponse<ReleasesSummaryResponse>> Releases,
    [property: JsonPropertyName("matches")] BucketsResponse<BucketResponse<MatchesSummaryResponse>> Matches,
    [property: JsonPropertyName("clearanceRequests")]
        BucketsResponse<BucketResponse<ClearanceRequestsSummaryBucketResponse>> ClearanceRequests,
    [property: JsonPropertyName("notifications")]
        BucketsResponse<BucketResponse<NotificationsSummaryResponse>> Notifications
);

public record BucketsResponse<T>([property: JsonPropertyName("buckets")] IReadOnlyList<T> Buckets);
