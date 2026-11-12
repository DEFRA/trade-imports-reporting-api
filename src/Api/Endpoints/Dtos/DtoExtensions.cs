using System.Globalization;
using CsvHelper;
using Defra.TradeImportsReportingApi.Api.Data;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public static class DtoExtensions
{
    public static ReleasesSummaryResponse ToResponse(this ReleasesSummary summary) =>
        new(summary.Automatic, summary.Manual, summary.Total);

    public static MatchesSummaryResponse ToResponse(this MatchesSummary summary) =>
        new(summary.Match, summary.NoMatch, summary.Total);

    public static MatchesSummaryByLevelResponse ToResponse(this MatchesSummaryByLevel summaryByLevel) =>
        new(summaryByLevel.Level1, summaryByLevel.Level2, summaryByLevel.Level3, summaryByLevel.Total);

    public static ClearanceRequestsSummaryResponse ToResponse(this ClearanceRequestsSummary summary) =>
        new(summary.Unique, summary.Total);

    public static NotificationsSummaryResponse ToResponse(this NotificationsSummary summary) =>
        new(summary.ChedA, summary.ChedP, summary.ChedPP, summary.ChedD, summary.Total);

    public static ChedReservationsSummaryResponse ToResponse(this ChedReservationsSummary summary) =>
        new(
            summary.Unreserved,
            summary.Reserved,
            summary.Consumed,
            summary.Unsuccessful,
            summary.Deleted,
            summary.Total
        );

    public static IntervalsResponse<IntervalResponse<ReleasesSummaryResponse>> ToResponse(
        this IReadOnlyList<ReleasesBucket> buckets
    ) =>
        new(
            buckets
                .Select(x => new IntervalResponse<ReleasesSummaryResponse>(x.Bucket, x.Summary.ToResponse()))
                .ToList()
        );

    public static IntervalsResponse<IntervalResponse<MatchesSummaryResponse>> ToResponse(
        this IReadOnlyList<MatchesBucket> buckets
    ) =>
        new(
            buckets.Select(x => new IntervalResponse<MatchesSummaryResponse>(x.Bucket, x.Summary.ToResponse())).ToList()
        );

    public static IntervalsResponse<IntervalResponse<ClearanceRequestsSummaryResponse>> ToResponse(
        this IReadOnlyList<ClearanceRequestsBucket> buckets
    ) =>
        new(
            buckets
                .Select(x => new IntervalResponse<ClearanceRequestsSummaryResponse>(x.Bucket, x.Summary.ToResponse()))
                .ToList()
        );

    public static IntervalsResponse<IntervalResponse<NotificationsSummaryResponse>> ToResponse(
        this IReadOnlyList<NotificationsBucket> buckets
    ) =>
        new(
            buckets
                .Select(x => new IntervalResponse<NotificationsSummaryResponse>(x.Bucket, x.Summary.ToResponse()))
                .ToList()
        );

    public static DatumResponse<MatchResponse> ToResponse(this IReadOnlyList<MatchResponse> data) => new(data);

    public static DatumResponse<ReleaseResponse> ToResponse(this IReadOnlyList<ReleaseResponse> data) => new(data);

    public static LastReceivedResponse ToResponse(this LastReceivedSummary lastReceived) =>
        new(
            lastReceived.Finalisation is not null
                ? new LastMessageResponse(lastReceived.Finalisation.Timestamp, lastReceived.Finalisation.Reference)
                : null,
            lastReceived.Request is not null
                ? new LastMessageResponse(lastReceived.Request.Timestamp, lastReceived.Request.Reference)
                : null,
            lastReceived.Notification is not null
                ? new LastMessageResponse(lastReceived.Notification.Timestamp, lastReceived.Notification.Reference)
                : null,
            lastReceived.TracesChed is not null
                ? new LastMessageResponse(lastReceived.TracesChed.Timestamp, lastReceived.TracesChed.Reference)
                : null,
            lastReceived.ChedReservation is not null
                ? new LastMessageResponse(
                    lastReceived.ChedReservation.Timestamp,
                    lastReceived.ChedReservation.Reference
                )
                : null
        );

    public static LastSentResponse ToResponse(this LastSentSummary lastSent) =>
        new(
            lastSent.Decision is not null
                ? new LastMessageResponse(lastSent.Decision.Timestamp, lastSent.Decision.Reference)
                : null
        );

    public static LastCreatedResponse ToResponse(this LastCreatedSummary lastCreated) =>
        new(
            lastCreated.Decision is not null
                ? new LastMessageResponse(lastCreated.Decision.Timestamp, lastCreated.Decision.Reference)
                : null
        );

    public static string ToCsvResponse(this IReadOnlyList<MatchResponse> data) =>
        data.ToCsvResponse<MatchResponse, MatchResponse.MatchResponseMap>();

    public static string ToCsvResponse(this IReadOnlyList<ReleaseResponse> data) =>
        data.ToCsvResponse<ReleaseResponse, ReleaseResponse.ReleaseResponseMap>();

    public static string ToCsvResponse<TRow, TMap>(this IReadOnlyList<TRow> data)
        where TMap : CsvHelper.Configuration.ClassMap<TRow>
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.Context.RegisterClassMap<TMap>();
        csv.WriteRecords(data);

        return writer.ToString();
    }
}
