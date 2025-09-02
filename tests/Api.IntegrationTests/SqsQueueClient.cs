using System.Security.Cryptography;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests;

#pragma warning disable S3881
public class SqsQueueClient(string queueName) : IDisposable
#pragma warning restore S3881
{
    private readonly string _queueUrl = $"http://sqs.eu-west-2.127.0.0.1:4566/000000000000/{queueName}";
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

    public void Dispose() => _sqsClient.Dispose();

    public Task<ReceiveMessageResponse> ReceiveMessage() =>
        _sqsClient.ReceiveMessageAsync(
            new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 0,
            },
            CancellationToken.None
        );

    public Task<GetQueueAttributesResponse> GetQueueAttributes() =>
        _sqsClient.GetQueueAttributesAsync(
            new GetQueueAttributesRequest { AttributeNames = ["ApproximateNumberOfMessages"], QueueUrl = _queueUrl },
            CancellationToken.None
        );

    public async Task DrainAllMessages()
    {
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
            {
                var response = await ReceiveMessage();

                foreach (var message in response.Messages)
                {
                    await _sqsClient.DeleteMessageAsync(
                        new DeleteMessageRequest { QueueUrl = _queueUrl, ReceiptHandle = message.ReceiptHandle },
                        CancellationToken.None
                    );
                }

                var approximateNumberOfMessages = (await GetQueueAttributes()).ApproximateNumberOfMessages;

                return approximateNumberOfMessages == 0;
            })
        );
    }

    public async Task<string> SendMessage(
        string body,
        Dictionary<string, MessageAttributeValue>? messageAttributes = null,
        bool usesFifo = false,
        string? messageGroupId = null
    )
    {
        var request = new SendMessageRequest
        {
            MessageBody = body,
            MessageAttributes = messageAttributes,
            MessageDeduplicationId = usesFifo ? RandomNumberGenerator.GetString("abcdefg", 20) : null,
            MessageGroupId = usesFifo ? messageGroupId : null,
            QueueUrl = _queueUrl,
        };

        var result = await _sqsClient.SendMessageAsync(request, CancellationToken.None);

        return result.MessageId;
    }
}
