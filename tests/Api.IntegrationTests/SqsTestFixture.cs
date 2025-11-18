using System.Security.Cryptography;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Defra.TradeImportsReportingApi.Api.Extensions;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests;

// ReSharper disable once ClassNeverInstantiated.Global
public class SqsTestFixture : IAsyncLifetime
{
    private SqsQueueClient? _resourceEventsQueue;

    public SqsQueueClient ResourceEventsQueue => _resourceEventsQueue!;

    public Task InitializeAsync()
    {
        _resourceEventsQueue = new SqsQueueClient("trade_imports_data_upserted_reporting_api");

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _resourceEventsQueue?.Dispose();

        return Task.CompletedTask;
    }
}

[Collection("UsesSqs")]
public class SqsTestBase : IntegrationTestBase
{
    protected const string QueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/trade_imports_data_upserted_reporting_api";
    protected const string DeadLetterQueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/trade_imports_data_upserted_reporting_api-deadletter";

    private readonly AmazonSQSClient _sqsClient = new(
        new BasicAWSCredentials("test", "test"),
        new AmazonSQSConfig { AuthenticationRegion = "eu-west-2", ServiceURL = "http://localhost:4566" }
    );

    protected Task PurgeQueue(string queueUrl)
    {
        return _sqsClient.PurgeQueueAsync(queueUrl, CancellationToken.None);
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

        return result.MessageId;
    }

    protected Task<GetQueueAttributesResponse> GetQueueAttributes(string queueUrl)
    {
        return _sqsClient.GetQueueAttributesAsync(
            new GetQueueAttributesRequest { AttributeNames = ["ApproximateNumberOfMessages"], QueueUrl = queueUrl },
            CancellationToken.None
        );
    }

    protected static Dictionary<string, MessageAttributeValue> WithResourceEventAttributes(
        string resourceType,
        string? subResourceType,
        string resourceId
    )
    {
        var messageAttributes = new Dictionary<string, MessageAttributeValue>
        {
            {
                MessageBusHeaders.ResourceType,
                new MessageAttributeValue { DataType = "String", StringValue = resourceType }
            },
            {
                MessageBusHeaders.ResourceId,
                new MessageAttributeValue { DataType = "String", StringValue = resourceId }
            },
        };

        if (subResourceType != null)
        {
            messageAttributes.Add(
                MessageBusHeaders.SubResourceType,
                new MessageAttributeValue { DataType = "String", StringValue = subResourceType }
            );
        }

        return messageAttributes;
    }
}

[CollectionDefinition("UsesSqs")]
public class SqsTestFixtureCollection : ICollectionFixture<SqsTestFixture>;
