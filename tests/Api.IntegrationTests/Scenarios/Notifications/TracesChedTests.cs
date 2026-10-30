using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Models;
using MongoDB.Driver;
using Trade.Gateway.Api.Contract.Certificate;
using Assert = Xunit.Assert;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.Notifications;

public class TracesChedTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Fact]
    public async Task WhenTracesChedReceived_ShouldStoreStatusAndCommodities()
    {
        var ched = $"CHEDA.GB.2025.{Random.Shared.Next(1_000_000, 9_999_999)}";
        var created = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        var resourceEvent = CreateResourceEvent(
            ched,
            ResourceEventResourceTypes.TracesChed,
            new TracesChedEvent
            {
                Id = ched,
                Created = created,
                Updated = created.AddMinutes(1),
                Ched = new DefraUNVTDCHEDProfile
                {
                    ExchangedDocument = new ExchangedDocument
                    {
                        Identifier = ched,
                        DocumentStatusCode = TracesNotificationStatus.Submitted,
                    },
                    SpecifiedConsignment = new Consignment
                    {
                        IncludedConsignmentItem =
                        [
                            new ConsignmentItem
                            {
                                IncludedTradeLineItem =
                                [
                                    CreateTradeLineItem("0101210000", "Horses"),
                                    CreateTradeLineItem("0102291000", "Cattle"),
                                ],
                            },
                        ],
                    },
                },
            }
        );

        await SendTracesChedMessage(resourceEvent, CreateMessageAttributes(resourceEvent));

        var filter = Builders<TracesChed>.Filter.Eq(x => x.Id, ched);
        Assert.True(await AsyncWaiter.WaitForAsync(async () => await TracesCheds.CountDocumentsAsync(filter) == 1));

        var stored = await TracesCheds.Find(filter).SingleAsync();

        await Verify(stored, JsonVerifySettings).ScrubMember(nameof(TracesChed.Id));
    }

    private static TradeLineItem CreateTradeLineItem(string code, string description) =>
        new()
        {
            ApplicableClassification =
            [
                new ApplicableClassification
                {
                    ClassCode = new CodedValue { Value = code },
                    ClassName = [description],
                },
            ],
        };
}
