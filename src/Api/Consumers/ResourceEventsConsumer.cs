using Defra.TradeImportsReportingApi.Api.Extensions;
using SlimMessageBus;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

public class ResourceEventsConsumer(IConsumerContext context, ILogger<ResourceEventsConsumer> logger)
    : IConsumer<string>
{
    public Task OnHandle(string received, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Resource events consumer: {ResourceType} {SubResourceType}",
            context.GetResourceType(),
            context.GetSubResourceType()
        );

        return Task.CompletedTask;
    }
}
