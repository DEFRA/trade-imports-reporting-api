using Defra.TradeImportsReportingApi.Api.Utils.CorrelationId;

namespace Defra.TradeImportsReportingApi.Api.Tests
{
    internal class TestCorrelationIdGenerator(string value) : ICorrelationIdGenerator
    {
        public string Generate()
        {
            return value;
        }
    }
}
