using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Extensions;
using Defra.TradeImportsReportingApi.Api.Utils;
using SlimMessageBus;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

[ExcludeFromCodeCoverage]
public class ResourceEventsConsumer(
    IConsumerContext context,
    ILogger<ResourceEventsConsumer> logger,
    IDbContext dbContext
) : IConsumer<string>
{
    public async Task OnHandle(string received, CancellationToken cancellationToken)
    {
        var resourceType = context.GetResourceType();
        var subResourceType = context.GetSubResourceType();

        logger.LogInformation(
            "Resource events consumer: {ResourceType} {SubResourceType}",
            resourceType,
            subResourceType
        );

        if (
            resourceType == ResourceEventResourceTypes.CustomsDeclaration
            && subResourceType == ResourceEventSubResourceTypes.Finalisation
        )
        {
            await HandleFinalisation(received, cancellationToken);
        }
    }

    private async Task HandleFinalisation(string received, CancellationToken cancellationToken)
    {
        var customsDeclaration =
            MessageDeserializer.Deserialize<ResourceEvent<CustomsDeclaration>>(
                received,
                context.Headers.GetContentEncoding()
            ) ?? throw new InvalidOperationException("Failed to deserialize message");

        if (customsDeclaration.Resource?.Finalisation is null)
            throw new InvalidOperationException("Finalisation is null");

        var finalisation = customsDeclaration.Resource.Finalisation.ToFinalisation(customsDeclaration.ResourceId);
        if (finalisation.ReleaseType is not ReleaseType.Automatic && finalisation.ReleaseType is not ReleaseType.Manual)
        {
            logger.LogWarning(
                "Finalisation for {Mrn} was ReleaseType of {ReleaseType}, ignoring",
                finalisation.Mrn,
                finalisation.ReleaseType
            );
            return;
        }

        await dbContext.Finalisations.InsertOneAsync(finalisation, cancellationToken: cancellationToken);
    }
}
