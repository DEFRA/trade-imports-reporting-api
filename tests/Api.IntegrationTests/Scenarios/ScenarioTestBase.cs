using System.Text.Json;
using Amazon.SQS.Model;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Extensions;
using MongoDB.Driver;

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
}
