using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record LastReceivedResponse(
    [property: JsonPropertyName("finalisation")] LastMessageResponse? Finalisation,
    [property: JsonPropertyName("clearanceRequest")] LastMessageResponse? Request,
    [property: JsonPropertyName("preNotification")] LastMessageResponse? Notification
);
