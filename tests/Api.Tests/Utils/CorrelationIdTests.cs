using Defra.TradeImportsReportingApi.Api.Utils.CorrelationId;

namespace Defra.TradeImportsReportingApi.Api.Tests.Utils;

public class CorrelationIdTests
{
    [Fact]
    public void CorrelationId_ShouldBeGenerated()
    {
        var generator = new CorrelationIdGenerator();

        var id = generator.Generate();

        id.Length.Should().Be(20);
    }
}
