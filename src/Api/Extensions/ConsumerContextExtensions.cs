using System.Diagnostics.CodeAnalysis;
using Amazon.SQS.Model;
using SlimMessageBus;

namespace Defra.TradeImportsReportingApi.Api.Extensions;

[ExcludeFromCodeCoverage]
public static class MessageBusHeaders
{
    public const string ResourceTypeHeader = "ResourceType";
    public const string SubResourceTypeHeader = "SubResourceType";
    public const string SqsBusMessage = "Sqs_Message";
    public const string ResourceId = "ResourceId";
    public const string TraceId = "x-cdp-request-id";
    public const string ContentEncoding = "Content-Encoding";
}

[ExcludeFromCodeCoverage]
public static class ResourceTypes
{
    public const string Unknown = "Unknown";
    public const string ImportPreNotification = nameof(TradeImportsDataApi.Domain.Ipaffs.ImportPreNotification);
    public const string CustomsDeclaration = nameof(TradeImportsDataApi.Domain.CustomsDeclaration);
    public const string ProcessingError = nameof(TradeImportsDataApi.Domain.Errors.ProcessingError);
}

[ExcludeFromCodeCoverage]
public static class ConsumerContextExtensions
{
    public static string GetMessageId(this IConsumerContext consumerContext)
    {
        return consumerContext.Properties.TryGetValue(MessageBusHeaders.SqsBusMessage, out var sqsMessage)
            ? ((Message)sqsMessage).MessageId
            : string.Empty;
    }

    public static string GetResourceType(this IConsumerContext consumerContext)
    {
        if (consumerContext.Headers.TryGetValue(MessageBusHeaders.ResourceTypeHeader, out var resourceTypeValue))
        {
            return resourceTypeValue.ToString()! switch
            {
                ResourceTypes.CustomsDeclaration => ResourceTypes.CustomsDeclaration,
                ResourceTypes.ImportPreNotification => ResourceTypes.ImportPreNotification,
                ResourceTypes.ProcessingError => ResourceTypes.ProcessingError,
                _ => ResourceTypes.Unknown,
            };
        }

        return ResourceTypes.Unknown;
    }

    public static string GetResourceId(this IConsumerContext consumerContext)
    {
        if (consumerContext.Headers.TryGetValue(MessageBusHeaders.ResourceId, out var value))
        {
            return value.ToString()!;
        }

        return string.Empty;
    }
}
