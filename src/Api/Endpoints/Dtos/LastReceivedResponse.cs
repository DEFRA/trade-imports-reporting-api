using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record LastReceivedResponse(
    [property: JsonPropertyName("finalisation")] LastMessageResponse? Finalisation,
    [property: JsonPropertyName("request")] LastMessageResponse? Request,
    [property: JsonPropertyName("notification")] LastMessageResponse? Notification
);
