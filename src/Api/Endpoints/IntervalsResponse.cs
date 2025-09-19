using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public record IntervalsResponse(
    [property: JsonPropertyName("releases")] IntervalsResponse<IntervalResponse<ReleasesSummaryResponse>> Releases,
    [property: JsonPropertyName("matches")] IntervalsResponse<IntervalResponse<MatchesSummaryResponse>> Matches,
    [property: JsonPropertyName("clearanceRequests")]
        IntervalsResponse<IntervalResponse<ClearanceRequestsSummaryIntervalResponse>> ClearanceRequests,
    [property: JsonPropertyName("notifications")]
        IntervalsResponse<IntervalResponse<NotificationsSummaryResponse>> Notifications
);

public record IntervalsResponse<T>([property: JsonPropertyName("intervals")] IReadOnlyList<T> Intervals);
