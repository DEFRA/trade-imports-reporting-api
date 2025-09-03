using Defra.TradeImportsReportingApi.Api.Consumers;
using Defra.TradeImportsReportingApi.Api.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using SlimMessageBus.Host;

namespace Defra.TradeImportsReportingApi.Api.Tests.Consumers;

public class ResourceEventsConsumerTests
{
    [Fact]
    public async Task OnHandle_AsExpected()
    {
        var subject = new ResourceEventsConsumer(
            new ConsumerContext
            {
                Headers = new Dictionary<string, object>
                {
                    { MessageBusHeaders.ResourceType, Guid.NewGuid().ToString() },
                },
            },
            NullLogger<ResourceEventsConsumer>.Instance
        );

        var act = () => subject.OnHandle("[json]", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
