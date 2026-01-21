using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsReportingApi.Api.Authentication;
using Defra.TradeImportsReportingApi.Api.Configuration;
using Defra.TradeImportsReportingApi.Api.Services.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Admin;

[ExcludeFromCodeCoverage(
    Justification = "This is covered by Integration tests, which doesn't pick up on code coverage"
)]
public static class EndpointRouteBuilderExtensions
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("admin/dlq/resource-events/redrive", RedriveResourceEvents)
            .WithName(nameof(RedriveResourceEvents))
            .WithTags("Admin")
            .WithSummary("Initiates redrive of messages from the dead letter queue")
            .WithDescription("Redrives all messages on the resource events dead letter queue")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status405MethodNotAllowed)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(PolicyNames.Execute);

        app.MapPost("admin/dlq/resource-events/remove-message", RemoveResourceEventMessage)
            .WithName(nameof(RemoveResourceEventMessage))
            .WithTags("Admin")
            .WithSummary("Initiates removal of message from the dead letter queue")
            .WithDescription(
                "Attempts to find and remove a message on the resource events dead letter queue by message ID"
            )
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status405MethodNotAllowed)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(PolicyNames.Execute);

        app.MapPost("admin/dlq/resource-events/drain", DrainResourceEvents)
            .WithName(nameof(DrainResourceEvents))
            .WithTags("Admin")
            .WithSummary("Initiates drain of all messages from the dead letter queue")
            .WithDescription("Drains all messages on the resource events dead letter queue")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status405MethodNotAllowed)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(PolicyNames.Execute);

        app.MapPost("admin/dlq/activity-events/redrive", RedriveActivityEvents)
            .WithName(nameof(RedriveActivityEvents))
            .WithTags("Admin")
            .WithSummary("Initiates redrive of messages from the activity events dead letter queue")
            .WithDescription("Redrives all messages on the activity events dead letter queue")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status405MethodNotAllowed)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(PolicyNames.Execute);

        app.MapPost("admin/dlq/activity-events/remove-message", RemoveActivityEventMessage)
            .WithName(nameof(RemoveActivityEventMessage))
            .WithTags("Admin")
            .WithSummary("Initiates removal of message from the activity events dead letter queue")
            .WithDescription(
                "Attempts to find and remove a message on the activity events dead letter queue by message ID"
            )
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status405MethodNotAllowed)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(PolicyNames.Execute);

        app.MapPost("admin/dlq/activity-events/drain", DrainActivityEvents)
            .WithName(nameof(DrainActivityEvents))
            .WithTags("Admin")
            .WithSummary("Initiates drain of all messages from the dead letter queue")
            .WithDescription("Drains all messages on the activity events dead letter queue")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status405MethodNotAllowed)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(PolicyNames.Execute);
    }

    [HttpPost]
    private static async Task<IResult> RedriveResourceEvents(
        [FromServices] ISqsDeadLetterService deadLetterService,
        [FromServices] IOptions<ResourceEventsConsumerOptions> options,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (
                !await deadLetterService.Redrive(
                    options.Value.DeadLetterQueueName,
                    options.Value.QueueName,
                    cancellationToken
                )
            )
            {
                return Results.InternalServerError();
            }
        }
        catch (Exception)
        {
            return Results.InternalServerError();
        }

        return Results.Accepted();
    }

    [HttpPost]
    private static async Task<IResult> RemoveResourceEventMessage(
        string messageId,
        [FromServices] ISqsDeadLetterService deadLetterService,
        [FromServices] IOptions<ResourceEventsConsumerOptions> options,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await deadLetterService.Remove(
                messageId,
                options.Value.DeadLetterQueueName,
                cancellationToken
            );

            return Results.Content(result, "text/plain; charset=utf-8");
        }
        catch (Exception)
        {
            return Results.InternalServerError();
        }
    }

    [HttpPost]
    private static async Task<IResult> DrainResourceEvents(
        [FromServices] ISqsDeadLetterService deadLetterService,
        [FromServices] IOptions<ResourceEventsConsumerOptions> options,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (!await deadLetterService.Drain(options.Value.DeadLetterQueueName, cancellationToken))
            {
                return Results.InternalServerError();
            }
        }
        catch (Exception)
        {
            return Results.InternalServerError();
        }

        return Results.Ok();
    }

    [HttpPost]
    private static async Task<IResult> RedriveActivityEvents(
        [FromServices] ISqsDeadLetterService deadLetterService,
        [FromServices] IOptions<ActivityEventsConsumerOptions> options,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (
                !await deadLetterService.Redrive(
                    options.Value.DeadLetterQueueName,
                    options.Value.QueueName,
                    cancellationToken
                )
            )
            {
                return Results.InternalServerError();
            }
        }
        catch (Exception)
        {
            return Results.InternalServerError();
        }

        return Results.Accepted();
    }

    [HttpPost]
    private static async Task<IResult> RemoveActivityEventMessage(
        string messageId,
        [FromServices] ISqsDeadLetterService deadLetterService,
        [FromServices] IOptions<ActivityEventsConsumerOptions> options,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await deadLetterService.Remove(
                messageId,
                options.Value.DeadLetterQueueName,
                cancellationToken
            );

            return Results.Content(result, "text/plain; charset=utf-8");
        }
        catch (Exception)
        {
            return Results.InternalServerError();
        }
    }

    [HttpPost]
    private static async Task<IResult> DrainActivityEvents(
        [FromServices] ISqsDeadLetterService deadLetterService,
        [FromServices] IOptions<ActivityEventsConsumerOptions> options,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (!await deadLetterService.Drain(options.Value.DeadLetterQueueName, cancellationToken))
            {
                return Results.InternalServerError();
            }
        }
        catch (Exception)
        {
            return Results.InternalServerError();
        }

        return Results.Ok();
    }
}
