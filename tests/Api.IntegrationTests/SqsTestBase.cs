using System.Security.Cryptography;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Defra.TradeImportsReportingApi.Api.IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests;

public class SqsTestBase(ITestOutputHelper output) : TestBase
{
    protected const string ResourceEventsQueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/trade_imports_data_upserted_reporting_api";

    private readonly AmazonSQSClient _sqsClient = new(
        new BasicAWSCredentials("test", "test"),
        new AmazonSQSConfig
        {
            AuthenticationRegion = "eu-west-2",
            ServiceURL = "http://localhost:4566",
            Timeout = TimeSpan.FromSeconds(5),
            MaxErrorRetry = 0,
        }
    );

    private Task<ReceiveMessageResponse> ReceiveMessage(string queueUrl)
    {
        return _sqsClient.ReceiveMessageAsync(
            new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 0,
            },
            CancellationToken.None
        );
    }

    protected Task<GetQueueAttributesResponse> GetQueueAttributes(string queueUrl)
    {
        return _sqsClient.GetQueueAttributesAsync(
            new GetQueueAttributesRequest { AttributeNames = ["ApproximateNumberOfMessages"], QueueUrl = queueUrl },
            CancellationToken.None
        );
    }

    protected async Task DrainAllMessages(string queueUrl)
    {
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
            {
                var response = await ReceiveMessage(queueUrl);

                foreach (var message in response.Messages)
                {
                    output?.WriteLine("Drain message: {0} {1}", message.MessageId, message.Body);

                    await _sqsClient.DeleteMessageAsync(
                        new DeleteMessageRequest { QueueUrl = queueUrl, ReceiptHandle = message.ReceiptHandle },
                        CancellationToken.None
                    );
                }

                var approximateNumberOfMessages = (await GetQueueAttributes(queueUrl)).ApproximateNumberOfMessages;

                output?.WriteLine("ApproximateNumberOfMessages: {0}", approximateNumberOfMessages);

                return approximateNumberOfMessages == 0;
            })
        );
    }

    protected async Task<string> SendMessage(
        string messageGroupId,
        string body,
        string queueUrl,
        Dictionary<string, MessageAttributeValue>? messageAttributes = null,
        bool usesFifo = true
    )
    {
        var request = new SendMessageRequest
        {
            MessageAttributes = messageAttributes,
            MessageBody = body,
            MessageDeduplicationId = usesFifo ? RandomNumberGenerator.GetString("abcdefg", 20) : null,
            MessageGroupId = usesFifo ? messageGroupId : null,
            QueueUrl = queueUrl,
        };

        var result = await _sqsClient.SendMessageAsync(request, CancellationToken.None);

        output.WriteLine("Sent {0} to {1}", result.MessageId, queueUrl);

        return result.MessageId;
    }
}
