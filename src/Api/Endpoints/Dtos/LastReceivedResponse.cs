using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record LastReceivedResponse(
    [property: JsonPropertyName("finalisation")] LastReceivedMessageResponse? Finalisation,
    [property: JsonPropertyName("request")] LastReceivedMessageResponse? Request
);
