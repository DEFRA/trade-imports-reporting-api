using System.Text;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Data.Entities;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public static class DtoExtensions
{
    public static ReleasesSummaryResponse ToResponse(this ReleasesSummary summary) =>
        new(summary.Automatic, summary.Manual, summary.Total);

    public static MatchesSummaryResponse ToResponse(this MatchesSummary summary) =>
        new(summary.Match, summary.NoMatch, summary.Total);

    public static ClearanceRequestsSummaryResponse ToResponse(this ClearanceRequestsSummary summary) =>
        new(summary.Unique, summary.Total);

    public static NotificationsSummaryResponse ToResponse(this NotificationsSummary summary) =>
        new(summary.ChedA, summary.ChedP, summary.ChedPP, summary.ChedD, summary.Total);

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

    public static DatumResponse<MatchResponse> ToResponse(this IReadOnlyList<Decision> matches) =>
        new(matches.Select(x => new MatchResponse(x.Timestamp, x.Mrn)).ToList());

    public static DatumResponse<ReleasesResponse> ToResponse(this IReadOnlyList<Finalisation> finalisations) =>
        new(finalisations.Select(x => new ReleasesResponse(x.Timestamp, x.Mrn)).ToList());

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

    public static string ToCsvResponse(this IReadOnlyList<Decision> matches)
    {
        var csv = new StringBuilder();

        foreach (var decision in matches)
        {
            csv.AppendLine($"{decision.Timestamp:O},{EscapeCsv(decision.Mrn)}");
        }

        return csv.ToString();
    }

    public static string ToCsvResponse(this IReadOnlyList<Finalisation> finalisations)
    {
        var csv = new StringBuilder();

        foreach (var finalisation in finalisations)
        {
            csv.AppendLine($"{finalisation.Timestamp:O},{EscapeCsv(finalisation.Mrn)}");
        }

        return csv.ToString();
    }

    private static string EscapeCsv(string input)
    {
        var needsQuotes = input.Contains(',') || input.Contains('"') || input.Contains('\n') || input.Contains('\r');
        var escaped = input.Replace("\"", "\"\"");

        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }
}
