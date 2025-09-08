using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Extensions;
using Defra.TradeImportsReportingApi.Api.Models;
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

        if (
            resourceType == ResourceEventResourceTypes.CustomsDeclaration
            && subResourceType == ResourceEventSubResourceTypes.ClearanceDecision
        )
        {
            await HandleDecision(received, cancellationToken);
        }

        if (
            resourceType == ResourceEventResourceTypes.CustomsDeclaration
            && subResourceType == ResourceEventSubResourceTypes.ClearanceRequest
        )
        {
            await HandleRequest(received, cancellationToken);
        }

        if (resourceType == ResourceEventResourceTypes.ImportPreNotification)
        {
            await HandleNotification(received, cancellationToken);
        }
    }

    private async Task HandleFinalisation(string received, CancellationToken cancellationToken)
    {
        var customsDeclaration = DeserializeReceived<CustomsDeclarationEntity>(received);
        if (customsDeclaration.Resource?.Finalisation is null)
            throw new InvalidOperationException("Finalisation is null");

        var incomingFinalisation = customsDeclaration.Resource.Finalisation;
        var entityFinalisation = incomingFinalisation.ToFinalisation(customsDeclaration.ResourceId);

        if (entityFinalisation.ShouldBeStored())
        {
            await dbContext.Finalisations.InsertOneAsync(entityFinalisation, cancellationToken: cancellationToken);
            return;
        }

        logger.LogInformation(
            "Finalisation ignored {MessageSentAt} {FinalState} {IsManualRelease}",
            incomingFinalisation.MessageSentAt,
            incomingFinalisation.FinalState,
            incomingFinalisation.IsManualRelease
        );
    }

    private async Task HandleDecision(string received, CancellationToken cancellationToken)
    {
        var customsDeclaration = DeserializeReceived<CustomsDeclarationEntity>(received);
        if (customsDeclaration.Resource?.ClearanceDecision is null)
            throw new InvalidOperationException("Decision is null");

        var incomingDecision = customsDeclaration.Resource.ClearanceDecision;
        var entityDecision = incomingDecision.ToDecision(
            customsDeclaration.ResourceId,
            customsDeclaration.Resource.Created
        );

        await dbContext.Decisions.InsertOneAsync(entityDecision, cancellationToken: cancellationToken);
    }

    private async Task HandleRequest(string received, CancellationToken cancellationToken)
    {
        var customsDeclaration = DeserializeReceived<CustomsDeclarationEntity>(received);
        if (customsDeclaration.Resource?.ClearanceRequest is null)
            throw new InvalidOperationException("Request is null");

        var incomingRequest = customsDeclaration.Resource.ClearanceRequest;
        var entityRequest = incomingRequest.ToRequest(customsDeclaration.ResourceId);

        await dbContext.Requests.InsertOneAsync(entityRequest, cancellationToken: cancellationToken);
    }

    private async Task HandleNotification(string received, CancellationToken cancellationToken)
    {
        var importPreNotification = DeserializeReceived<ImportPreNotificationEntity>(received);
        if (importPreNotification.Resource is null)
            throw new InvalidOperationException("Resource is null");

        var incomingNotification = importPreNotification.Resource.ImportPreNotification;
        var entityNotification = incomingNotification.ToNotification(
            importPreNotification.ResourceId,
            importPreNotification.Resource.Created,
            importPreNotification.Resource.Updated
        );

        if (entityNotification.ShouldBeStored())
        {
            await dbContext.Notifications.InsertOneAsync(entityNotification, cancellationToken: cancellationToken);
            return;
        }

        logger.LogInformation(
            "Notification ignored {ImportNotificationType}",
            incomingNotification.ImportNotificationType
        );
    }

    private ResourceEvent<T> DeserializeReceived<T>(string received) =>
        MessageDeserializer.Deserialize<ResourceEvent<T>>(received, context.Headers.GetContentEncoding())
        ?? throw new InvalidOperationException("Failed to deserialize message");
}
