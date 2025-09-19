using System.Text.Json.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public record DatumResponse<T>([property: JsonPropertyName("data")] IReadOnlyList<T> Data);
