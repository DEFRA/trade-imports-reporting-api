using SlimMessageBus;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

public class ResourceEventsConsumer : IConsumer<string>, IConsumerWithContext
{
    public IConsumerContext Context { get; set; } = null!;

    public Task OnHandle(string received, CancellationToken cancellationToken)
    {
        // Check resource type using Context.GetResourceType()
        // Deserialise using MessageDeserializer.Deserialize
        // Convert to message type
        // Handle

        return Task.CompletedTask;
    }
}
