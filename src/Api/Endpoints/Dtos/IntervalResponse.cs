using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record IntervalResponse<T>(
    [property: JsonPropertyName("interval")] DateTime Interval,
    [property: JsonPropertyName("summary")] T Summary
);
