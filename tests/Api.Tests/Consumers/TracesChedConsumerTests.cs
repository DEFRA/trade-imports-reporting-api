using System.Text.Json;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Consumers;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Models;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using NSubstitute;
using SlimMessageBus;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsReportingApi.Api.Tests.Consumers;

public class TracesChedConsumerTests
{
    private static readonly DateTime s_issued = new(2025, 7, 3, 13, 42, 0, DateTimeKind.Utc);
    private static readonly DateTime s_lastUpdated = s_issued.AddHours(1);

    [Fact]
    public async Task OnHandle_DataApiPayload_ShouldInsertAsExpected()
    {
        var tracesCheds = Substitute.For<IMongoCollection<TracesChed>>();
        var dbContext = Substitute.For<IDbContext>();
        dbContext.TracesCheds.Returns(tracesCheds);

        var subject = new TracesChedConsumer(CreateContext(), NullLogger<TracesChedConsumer>.Instance, dbContext);

        await subject.OnHandle(CreatePayload(), CancellationToken.None);

        await tracesCheds
            .Received(1)
            .InsertOneAsync(
                Arg.Is<TracesChed>(x =>
                    x.ReferenceNumber == "ched"
                    && x.NotificationType == NotificationType.ChedA
                    && x.ChedCreated == s_issued
                    && x.Timestamp == s_lastUpdated
                ),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>()
            );
    }

    private static IConsumerContext CreateContext()
    {
        var context = Substitute.For<IConsumerContext>();
        context.Headers.Returns(new Dictionary<string, object>());

        return context;
    }

    private static string CreatePayload() =>
        JsonSerializer.Serialize(
            new ResourceEvent<TracesChedEvent>
            {
                ResourceId = "ched",
                ResourceType = "TracesChed",
                Operation = "Created",
                Resource = new TracesChedEvent
                {
                    Id = "id",
                    Created = s_issued,
                    Updated = s_lastUpdated,
                    Ched = new DefraUNVTDCHEDProfile
                    {
                        LastUpdated = s_lastUpdated,
                        ExchangedDocument = new ExchangedDocument
                        {
                            Identifier = "CHEDA.GB.2025.1234567",
                            IssueDateTime = s_issued,
                        },
                        SpecifiedConsignment = new Consignment(),
                    },
                },
            }
        );
}
