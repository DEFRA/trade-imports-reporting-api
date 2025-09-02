namespace Defra.TradeImportsReportingApi.Api.Utils.CorrelationId;

public class CorrelationIdGenerator : ICorrelationIdGenerator
{
    public string Generate()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Random.Shared.Next(1, 9999999);

        return $"{timestamp}{random:0000000}";
    }
}
