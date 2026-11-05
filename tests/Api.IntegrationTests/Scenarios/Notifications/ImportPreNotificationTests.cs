using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsReportingApi.Api.Models;
using MongoDB.Driver;
using Assert = Xunit.Assert;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.Notifications;

public class ImportPreNotificationTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Fact]
    public async Task WhenChedReceived_ShouldStoreStatusAndCommodities()
    {
        var ched = Guid.NewGuid().ToString();
        var created = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        var resourceEvent = CreateResourceEvent(
            ched,
            ResourceEventResourceTypes.ImportPreNotification,
            new ImportPreNotificationEvent
            {
                Id = "test",
                Created = created,
                Updated = created.AddMinutes(1),
                ImportPreNotification = new ImportPreNotification
                {
                    ImportNotificationType = ImportPreNotificationType.CVEDA,
                    Status = "SUBMITTED",
                    PartOne = new PartOne
                    {
                        Commodities = new Commodities
                        {
                            CommodityComplements =
                            [
                                new CommodityComplements
                                {
                                    CommodityId = "0101210000",
                                    CommodityDescription = "Horses",
                                },
                                new CommodityComplements
                                {
                                    CommodityId = "0102291000",
                                    CommodityDescription = "Cattle",
                                },
                            ],
                        },
                    },
                },
            }
        );

        await SendMessage(resourceEvent, CreateMessageAttributes(resourceEvent));

        var filter = Builders<Data.Entities.ImportPreNotification>.Filter.Eq(x => x.Id, ched);
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () => await ImportPreNotifications.CountDocumentsAsync(filter) == 1)
        );

        var stored = await ImportPreNotifications.Find(filter).SingleAsync();

        await Verify(stored, JsonVerifySettings).ScrubMember(nameof(Data.Entities.ImportPreNotification.Id));
    }
}
