using System.Text.Json;
using Amazon.SQS.Model;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Extensions;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Consumers;

public class ResourceEventsConsumerTests(SqsTestFixture sqsTestFixture) : SqsTestBase
{
    [Fact]
    public async Task DefaultConsumptionTest()
    {
        var resourceEvent = new ResourceEvent<CustomsDeclaration>
        {
            ResourceId = Guid.NewGuid().ToString(),
            ResourceType = ResourceEventResourceTypes.CustomsDeclaration,
            Operation = "Created",
        };

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

        await sqsTestFixture.ResourceEventsQueue.SendMessage(
            JsonSerializer.Serialize(resourceEvent),
            messageAttributes
        );

        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await sqsTestFixture.ResourceEventsQueue.GetQueueAttributes()).ApproximateNumberOfMessages == 0
            )
        );
    }
}
