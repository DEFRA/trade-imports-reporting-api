using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record SummaryResponse(
    [property: JsonPropertyName("releases")] ReleasesSummaryResponse Releases,
    [property: JsonPropertyName("matches")] MatchesSummaryResponse Matches,
    [property: JsonPropertyName("clearanceRequests")] ClearanceRequestsSummaryResponse ClearanceRequests,
    [property: JsonPropertyName("notifications")] NotificationsSummaryResponse Notifications
);
