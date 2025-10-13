using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record StatusResponse(
    [property: JsonPropertyName("received")] LastReceivedResponse Received,
    [property: JsonPropertyName("sent")] LastSentResponse Sent
);
