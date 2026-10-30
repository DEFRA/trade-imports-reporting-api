using System.Net;
using Defra.TradeImportsDataApi.Domain.Events;
using FluentAssertions;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Endpoints.Admin;

public class TracesChedEventsDrainTests : AdminTestBase
{
    protected const string QueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/trade_imports_tracesched_upserted_reporting_api";
    protected const string DeadLetterQueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/trade_imports_tracesched_upserted_reporting_api-deadletter";

    [Fact]
    public async Task When_message_processing_fails_and_moved_to_dlq_Then_dlq_can_be_drained()
    {
        const string ched = "CHEDA.GB.2025.0000001";

        await PurgeQueue(QueueUrl);
        await PurgeQueue(DeadLetterQueueUrl);

        await SendMessage(
            ched,
            "{}",
            DeadLetterQueueUrl,
            WithResourceEventAttributes<ResourceEvent<TracesChedEvent>>(
                ResourceEventResourceTypes.TracesChed,
                null,
                ched
            ),
            false
        );

        var messagesOnDeadLetterQueue = await AsyncWaiter.WaitForAsync(async () =>
            (await GetQueueAttributes(DeadLetterQueueUrl)).ApproximateNumberOfMessages == 1
        );
        Assert.True(messagesOnDeadLetterQueue, "Messages on dead letter queue was not drained");

        var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync(
            Testing.Endpoints.Admin.TracesChedEvents.DeadLetterQueue.Drain(),
            null
        );

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
}
