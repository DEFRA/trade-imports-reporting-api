using System.Text.Json;
using Amazon.SQS.Model;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Extensions;
using Defra.TradeImportsReportingApi.Api.Models;
using MongoDB.Driver;
using Decision = Defra.TradeImportsReportingApi.Api.Data.Entities.Decision;
using Finalisation = Defra.TradeImportsReportingApi.Api.Data.Entities.Finalisation;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios;

public class ScenarioTestBase(SqsTestFixture sqsTestFixture) : SqsTestBase, IAsyncLifetime
{
    public required IMongoCollection<Finalisation> Finalisations { get; set; }
    public required IMongoCollection<Decision> Decisions { get; set; }
    public required IMongoCollection<Request> Requests { get; set; }
    public required IMongoCollection<Notification> Notifications { get; set; }

    public async Task InitializeAsync()
    {
        Finalisations = GetMongoCollection<Finalisation>();
        Decisions = GetMongoCollection<Decision>();
        Requests = GetMongoCollection<Request>();
        Notifications = GetMongoCollection<Notification>();

        await Finalisations.DeleteManyAsync(FilterDefinition<Finalisation>.Empty);
        await Decisions.DeleteManyAsync(FilterDefinition<Decision>.Empty);
        await Requests.DeleteManyAsync(FilterDefinition<Request>.Empty);
        await Notifications.DeleteManyAsync(FilterDefinition<Notification>.Empty);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected static Dictionary<string, MessageAttributeValue> CreateMessageAttributes<T>(
        ResourceEvent<T> resourceEvent
    )
    {
        var messageAttributes = new Dictionary<string, MessageAttributeValue>
        {
            {
                MessageBusHeaders.ResourceId,
                new MessageAttributeValue { DataType = "String", StringValue = resourceEvent.ResourceId }
            },
            {
                MessageBusHeaders.ResourceType,
                new MessageAttributeValue { DataType = "String", StringValue = resourceEvent.ResourceType }
            },
        };

        if (resourceEvent.SubResourceType is not null)
        {
            messageAttributes.Add(
                MessageBusHeaders.SubResourceType,
                new MessageAttributeValue { DataType = "String", StringValue = resourceEvent.SubResourceType }
            );
        }

        return messageAttributes;
    }

    protected async Task SendMessage<T>(
        ResourceEvent<T> resourceEvent,
        Dictionary<string, MessageAttributeValue>? messageAttributes = null
    ) =>
        await sqsTestFixture.ResourceEventsQueue.SendMessage(
            JsonSerializer.Serialize(resourceEvent),
            messageAttributes
        );

    protected static ResourceEvent<T> CreateResourceEvent<T>(
        string resourceId,
        string resourceType,
        T resource,
        string? subResourceType = null
    ) =>
        new()
        {
            ResourceId = resourceId,
            ResourceType = resourceType,
            SubResourceType = subResourceType,
            Operation = "Created",
            Resource = resource,
        };

    protected async Task WaitForRequestMrn(string mrn, int count = 1)
    {
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
            {
                var requests = await Requests.FindAsync(Builders<Request>.Filter.Eq(x => x.Mrn, mrn));

                return (await requests.ToListAsync()).Count == count;
            })
        );
    }

    protected async Task WaitForDecisionMrn(string mrn, int count = 1)
    {
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
            {
                var decisions = await Decisions.FindAsync(Builders<Decision>.Filter.Eq(x => x.Mrn, mrn));

                return (await decisions.ToListAsync()).Count == count;
            })
        );
    }

    protected async Task WaitForFinalisationMrn(string mrn, int count = 1)
    {
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
            {
                var finalisations = await Finalisations.FindAsync(Builders<Finalisation>.Filter.Eq(x => x.Mrn, mrn));

                return (await finalisations.ToListAsync()).Count == count;
            })
        );
    }

    protected async Task WaitForNotificationChed(string ched, int count = 1)
    {
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
            {
                var notifications = await Notifications.FindAsync(
                    Builders<Notification>.Filter.Eq(x => x.ReferenceNumber, ched)
                );

                return (await notifications.ToListAsync()).Count == count;
            })
        );
    }

    protected static DateTime[] CreateIntervals(DateTime from, DateTime to, int numberRequired = 1)
    {
        var totalTicks = to.Ticks - from.Ticks;
        var denominator = numberRequired + 1;
        var result = new DateTime[numberRequired];

        for (var i = 1; i <= numberRequired; i++)
        {
            var ticks = from.Ticks + totalTicks * i / denominator;

            result[i - 1] = new DateTime(ticks, from.Kind);
        }

        return result;
    }

    protected async Task SendNotification(
        DateTime created,
        string? ched = null,
        DateTime? updated = null,
        string type = ImportPreNotificationType.CVEDA,
        bool wait = true
    )
    {
        ched ??= Guid.NewGuid().ToString();

        var resourceEvent = CreateResourceEvent(
            ched,
            ResourceEventResourceTypes.ImportPreNotification,
            new ImportPreNotificationEntity
            {
                Created = created,
                Updated = updated ?? created,
                ImportPreNotification = new ImportPreNotification { ImportNotificationType = type },
            }
        );

        await SendMessage(resourceEvent, CreateMessageAttributes(resourceEvent));

        if (wait)
            await WaitForNotificationChed(ched);
    }

    protected async Task SendClearanceRequest(DateTime messageSentAt, string? mrn = null, bool wait = true)
    {
        mrn ??= Guid.NewGuid().ToString();

        var resourceEvent = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity { ClearanceRequest = new ClearanceRequest { MessageSentAt = messageSentAt } },
            ResourceEventSubResourceTypes.ClearanceRequest
        );

        await SendMessage(resourceEvent, CreateMessageAttributes(resourceEvent));

        if (wait)
            await WaitForRequestMrn(mrn);
    }

    protected async Task SendDecision(
        DateTime mrnCreated,
        DateTime decisionCreated,
        string? mrn = null,
        string decisionCode = DecisionCode.NoMatch,
        bool wait = true
    )
    {
        mrn ??= Guid.NewGuid().ToString();

        var resourceEvent = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity
            {
                Created = mrnCreated,
                ClearanceDecision = new ClearanceDecision
                {
                    Created = decisionCreated,
                    Items =
                    [
                        new ClearanceDecisionItem
                        {
                            Checks = [new ClearanceDecisionCheck { CheckCode = "IGNORE", DecisionCode = decisionCode }],
                        },
                    ],
                },
            },
            ResourceEventSubResourceTypes.ClearanceDecision
        );

        await SendMessage(resourceEvent, CreateMessageAttributes(resourceEvent));

        if (wait)
            await WaitForDecisionMrn(mrn);
    }

    protected async Task SendFinalisation(
        DateTime messageSentAt,
        string? mrn = null,
        bool isCancelled = false,
        bool isManualRelease = false,
        bool wait = true
    )
    {
        mrn ??= Guid.NewGuid().ToString();

        var resourceEvent = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclaration
            {
                Finalisation = new Defra.TradeImportsDataApi.Domain.CustomsDeclaration.Finalisation
                {
                    ExternalVersion = 1,
                    FinalState = isCancelled ? "1" : "0",
                    IsManualRelease = isManualRelease,
                    MessageSentAt = messageSentAt,
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );

        await SendMessage(resourceEvent, CreateMessageAttributes(resourceEvent));

        if (wait)
            await WaitForFinalisationMrn(mrn);
    }
}
