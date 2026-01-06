using System.Net;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.IntegrationTests.TestUtils;
using FluentAssertions;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Endpoints.Admin;

public class ActivityEventsDrainTests : AdminTestBase
{
    protected const string QueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/trade_imports_activity_reporting_api";
    protected const string DeadLetterQueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/trade_imports_activity_reporting_api-deadletter";

    [Fact]
    public async Task When_message_processing_fails_and_moved_to_dlq_Then_dlq_can_be_drained()
    {
        var resourceEvent = FixtureTest.UsingContent("CustomsDeclarationClearanceDecisionResourceEvent.json");
        const string mrn = "25GB0XX00XXXXX0002";
        resourceEvent = resourceEvent.Replace("25GB0XX00XXXXX0000", mrn);

        await PurgeQueue(QueueUrl);
        await PurgeQueue(DeadLetterQueueUrl);

        await SendMessage(
            mrn,
            resourceEvent,
            DeadLetterQueueUrl,
            WithResourceEventAttributes<BtmsActivityEvent<BtmsToCdsActivity>>(
                "CustomsDeclaration",
                "ClearanceDecision",
                mrn
            ),
            false
        );

        var messagesOnDeadLetterQueue = await AsyncWaiter.WaitForAsync(async () =>
            (await GetQueueAttributes(DeadLetterQueueUrl)).ApproximateNumberOfMessages == 1
        );
        Assert.True(messagesOnDeadLetterQueue, "Messages on dead letter queue was not drained");

        var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync(Testing.Endpoints.Admin.ActivityEvents.DeadLetterQueue.Drain(), null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // We expect no messages on either queue following a drain
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(QueueUrl)).ApproximateNumberOfMessages == 0
            )
        );
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(DeadLetterQueueUrl)).ApproximateNumberOfMessages == 0
            )
        );
    }

    [Fact]
    public async Task When_message_processing_fails_and_moved_to_dlq_Then_message_can_be_redriven()
    {
        const string mrn = "25GB0XX00XXXXX0000";
        var resourceEvent = FixtureTest.UsingContent("CustomsDeclarationClearanceDecisionResourceEvent.json");

        await PurgeQueue(QueueUrl);
        await PurgeQueue(DeadLetterQueueUrl);

        await SendMessage(
            mrn,
            resourceEvent,
            DeadLetterQueueUrl,
            WithResourceEventAttributes<BtmsActivityEvent<BtmsToCdsActivity>>(
                "CustomsDeclaration",
                "ClearanceDecision",
                mrn
            ),
            false
        );

        var messagesOnDeadLetterQueue = await AsyncWaiter.WaitForAsync(async () =>
            (await GetQueueAttributes(DeadLetterQueueUrl)).ApproximateNumberOfMessages == 1
        );
        Assert.True(messagesOnDeadLetterQueue, "Messages on dead letter queue was not received");

        var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync(
            Testing.Endpoints.Admin.ActivityEvents.DeadLetterQueue.Redrive(),
            null
        );

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(QueueUrl)).ApproximateNumberOfMessages == 0
            )
        );

        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(DeadLetterQueueUrl)).ApproximateNumberOfMessages == 0
            )
        );
    }

    [Fact]
    public async Task When_message_processing_fails_and_moved_to_dlq_Then_message_can_be_removed()
    {
        var resourceEvent = FixtureTest.UsingContent("CustomsDeclarationClearanceDecisionResourceEvent.json");
        const string mrn = "25GB0XX00XXXXX0001";
        resourceEvent = resourceEvent.Replace("25GB0XX00XXXXX0000", mrn);

        await PurgeQueue(QueueUrl);
        await PurgeQueue(DeadLetterQueueUrl);

        var messageId = await SendMessage(
            mrn,
            resourceEvent,
            DeadLetterQueueUrl,
            WithResourceEventAttributes<BtmsActivityEvent<BtmsToCdsActivity>>(
                "CustomsDeclaration",
                "ClearanceDecision",
                mrn
            ),
            false
        );

        var messagesOnDeadLetterQueue = await AsyncWaiter.WaitForAsync(async () =>
            (await GetQueueAttributes(DeadLetterQueueUrl)).ApproximateNumberOfMessages == 1
        );
        Assert.True(messagesOnDeadLetterQueue, "Messages on dead letter queue was not received");

        var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync(
            Testing.Endpoints.Admin.ActivityEvents.DeadLetterQueue.RemoveMessage(messageId),
            null
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // We expect no messages on either queue following removal of the single message
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(QueueUrl)).ApproximateNumberOfMessages == 0
            )
        );
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(DeadLetterQueueUrl)).ApproximateNumberOfMessages == 0
            )
        );
    }
}
