using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Data.Entities;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public static class DtoExtensions
{
    public static ClearanceRequestsSummaryBucketResponse ToBucketResponse(this ClearanceRequestsSummary summary) =>
        new(summary.Unique);

    public static ReleasesSummaryResponse ToResponse(this ReleasesSummary summary) =>
        new(summary.Automatic, summary.Manual, summary.Total);

    public static MatchesSummaryResponse ToResponse(this MatchesSummary summary) =>
        new(summary.Match, summary.NoMatch, summary.Total);

    public static ClearanceRequestsSummaryResponse ToResponse(this ClearanceRequestsSummary summary) =>
        new(summary.Unique, summary.Total);

    public static NotificationsSummaryResponse ToResponse(this NotificationsSummary summary) =>
        new(summary.ChedA, summary.ChedP, summary.ChedPP, summary.ChedD, summary.Total);

    public static BucketsResponse<BucketResponse<ReleasesSummaryResponse>> ToResponse(
        this IReadOnlyList<ReleasesBucket> buckets
    ) =>
        new(
            buckets.Select(x => new BucketResponse<ReleasesSummaryResponse>(x.Bucket, x.Summary.ToResponse())).ToList()
        );

    public static BucketsResponse<BucketResponse<MatchesSummaryResponse>> ToResponse(
        this IReadOnlyList<MatchesBucket> buckets
    ) =>
        new(buckets.Select(x => new BucketResponse<MatchesSummaryResponse>(x.Bucket, x.Summary.ToResponse())).ToList());

    public static BucketsResponse<BucketResponse<ClearanceRequestsSummaryBucketResponse>> ToResponse(
        this IReadOnlyList<ClearanceRequestsBucket> buckets
    ) =>
        new(
            buckets
                .Select(x => new BucketResponse<ClearanceRequestsSummaryBucketResponse>(
                    x.Bucket,
                    x.Summary.ToBucketResponse()
                ))
                .ToList()
        );

    public static BucketsResponse<BucketResponse<NotificationsSummaryResponse>> ToResponse(
        this IReadOnlyList<NotificationsBucket> buckets
    ) =>
        new(
            buckets
                .Select(x => new BucketResponse<NotificationsSummaryResponse>(x.Bucket, x.Summary.ToResponse()))
                .ToList()
        );

    public static DatumResponse<MatchResponse> ToResponse(this IReadOnlyList<Decision> matches) =>
        new(matches.Select(x => new MatchResponse(x.Timestamp, x.Mrn)).ToList());
}
