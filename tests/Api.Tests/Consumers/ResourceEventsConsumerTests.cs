using Defra.TradeImportsReportingApi.Api.Consumers;

namespace Defra.TradeImportsReportingApi.Api.Tests.Consumers;

public class ResourceEventsConsumerTests
{
    [Fact]
    public async Task OnHandle_AsExpected()
    {
        var subject = new ResourceEventsConsumer();

        var act = () => subject.OnHandle("json", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
