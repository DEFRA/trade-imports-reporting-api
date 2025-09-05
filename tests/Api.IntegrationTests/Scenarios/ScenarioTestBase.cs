using System.Text.Json;
using Amazon.SQS.Model;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Extensions;
using MongoDB.Driver;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios;

public class ScenarioTestBase(SqsTestFixture sqsTestFixture) : SqsTestBase, IAsyncLifetime
{
    public required IMongoCollection<Data.Entities.Finalisation> Finalisations { get; set; }
    public required IMongoCollection<Data.Entities.Decision> Decisions { get; set; }
    public required IMongoCollection<Data.Entities.Request> Requests { get; set; }

    public async Task InitializeAsync()
    {
        Finalisations = GetMongoCollection<Data.Entities.Finalisation>();
        Decisions = GetMongoCollection<Data.Entities.Decision>();
        Requests = GetMongoCollection<Data.Entities.Request>();

        await Finalisations.DeleteManyAsync(FilterDefinition<Data.Entities.Finalisation>.Empty);
        await Decisions.DeleteManyAsync(FilterDefinition<Data.Entities.Decision>.Empty);
        await Requests.DeleteManyAsync(FilterDefinition<Data.Entities.Request>.Empty);
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
                var request = await Requests.FindAsync(Builders<Request>.Filter.Eq(x => x.Mrn, mrn));

                return (await request.ToListAsync()).Count == count;
            })
        );
    }

    protected async Task WaitForDecisionMrn(string mrn, int count = 1)
    {
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
            {
                var decision = await Decisions.FindAsync(Builders<Decision>.Filter.Eq(x => x.Mrn, mrn));

                return (await decision.ToListAsync()).Count == count;
            })
        );
    }

    protected async Task WaitForFinalisationMrn(string mrn, int count = 1)
    {
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
            {
                var finalisation = await Finalisations.FindAsync(
                    Builders<Data.Entities.Finalisation>.Filter.Eq(x => x.Mrn, mrn)
                );

                return (await finalisation.ToListAsync()).Count == count;
            })
        );
    }
}
