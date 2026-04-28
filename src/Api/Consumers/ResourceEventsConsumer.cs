using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Data.Extensions;
using Defra.TradeImportsReportingApi.Api.Extensions;
using Defra.TradeImportsReportingApi.Api.Utils;
using MongoDB.Driver;
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

        switch (resourceType)
        {
            case ResourceEventResourceTypes.CustomsDeclaration:
                var customsDeclaration = DeserializeReceived<CustomsDeclarationEvent>(received);
                await HandleCustomsDeclaration(customsDeclaration, cancellationToken);
                switch (subResourceType)
                {
                    case ResourceEventSubResourceTypes.Finalisation:
                        await HandleFinalisation(customsDeclaration, cancellationToken);
                        break;

                    case ResourceEventSubResourceTypes.ClearanceDecision:
                        await HandleDecision(customsDeclaration, cancellationToken);
                        break;

                    case ResourceEventSubResourceTypes.ClearanceRequest:
                        await HandleRequest(customsDeclaration, cancellationToken);
                        break;
                }
                break;

            case ResourceEventResourceTypes.ImportPreNotification:
                await HandleNotification(received, cancellationToken);
                break;
        }
    }

    private async Task HandleCustomsDeclaration(
        ResourceEvent<CustomsDeclarationEvent> customsDeclaration,
        CancellationToken cancellationToken
    )
    {
        if (customsDeclaration.Resource is null)
            throw new InvalidOperationException("Resource is null");

        var invalidClearanceInternalCodes =
            customsDeclaration
                .Resource.ClearanceDecision?.Results?.Where(result => result.InternalDecisionCodeIsUnknown())
                .Select(result => result.InternalDecisionCode!)
                .ToArray() ?? [];

        if (invalidClearanceInternalCodes.Length > 0)
        {
            logger.LogWarning(
                "Invalid internal clearance decision codes identified : {InvalidDecisionCodes}",
                string.Join(",", invalidClearanceInternalCodes)
            );
        }

        var entity = customsDeclaration.Resource.ToCustomsDeclaration();
        var filter = Builders<CustomsDeclaration>.Filter.Eq(x => x.Id, entity.Id);
        await dbContext.CustomsDeclarations.ReplaceOneAsync(
            filter,
            entity,
            new ReplaceOptions() { IsUpsert = true },
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleFinalisation(
        ResourceEvent<CustomsDeclarationEvent> customsDeclaration,
        CancellationToken cancellationToken
    )
    {
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

    private async Task HandleDecision(
        ResourceEvent<CustomsDeclarationEvent> customsDeclaration,
        CancellationToken cancellationToken
    )
    {
        if (customsDeclaration.Resource?.ClearanceDecision is null)
            throw new InvalidOperationException("Decision is null");

        var incomingDecision = customsDeclaration.Resource.ClearanceDecision;
        var entityDecision = incomingDecision.ToDecision(
            customsDeclaration.ResourceId,
            customsDeclaration.Resource.Created
        );

        await dbContext.Decisions.InsertOneAsync(entityDecision, cancellationToken: cancellationToken);
    }

    private async Task HandleRequest(
        ResourceEvent<CustomsDeclarationEvent> customsDeclaration,
        CancellationToken cancellationToken
    )
    {
        if (customsDeclaration.Resource?.ClearanceRequest is null)
            throw new InvalidOperationException("Request is null");

        var incomingRequest = customsDeclaration.Resource.ClearanceRequest;
        var entityRequest = incomingRequest.ToRequest(customsDeclaration.ResourceId);

        await dbContext.Requests.InsertOneAsync(entityRequest, cancellationToken: cancellationToken);
    }

    private async Task HandleNotification(string received, CancellationToken cancellationToken)
    {
        var importPreNotification = DeserializeReceived<ImportPreNotificationEvent>(received);
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
