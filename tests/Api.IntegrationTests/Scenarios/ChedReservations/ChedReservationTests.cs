using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Driver;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.ChedReservations;

public class ChedReservationTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    private static readonly DateTime s_timestamp = new(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

    [Fact]
    public async Task WhenMultipleReservationsForSameChedAndMrn_LatestShouldBeReturned()
    {
        var resourceId = "ched1_mrn1";

        await SendChedReservation(s_timestamp, "ched1", "mrn1");
        await SendChedReservation(s_timestamp.AddSeconds(10), "ched1", "mrn1");
        await WaitForChedReservation(resourceId, count: 2);

        await VerifyJson(await GetLastReceived(), JsonVerifySettings);
    }

    [Fact]
    public async Task WhenReservationDeleted_DeletionShouldBeAvailableAndBeLastReceived()
    {
        var resourceId = "ched1_mrn1";

        await SendChedReservation(s_timestamp, "ched1", "mrn1");
        await SendChedReservationDeleted(s_timestamp.AddSeconds(10), "ched1", "mrn1");
        await WaitForChedReservation(resourceId, count: 2);

        Assert.Equal(
            1,
            await ChedReservations.CountDocumentsAsync(
                Builders<ChedReservation>.Filter.Eq(x => x.Operation, ResourceEventOperations.Deleted)
            )
        );

        await VerifyJson(await GetLastReceived(), JsonVerifySettings);
    }

    [Fact]
    public async Task WhenOlderReservationArrivesAfterNewer_LatestShouldStillBeReturned()
    {
        var resourceId = "ched1_mrn1";

        await SendChedReservation(s_timestamp.AddSeconds(10), "ched1", "mrn1");
        await SendChedReservation(s_timestamp, "ched1", "mrn1");
        await WaitForChedReservation(resourceId, count: 2);

        await VerifyJson(await GetLastReceived(), JsonVerifySettings);
    }

    private static async Task<string> GetLastReceived()
    {
        var response = await DefaultClient.GetAsync(Testing.Endpoints.LastReceived.Get());

        return await response.Content.ReadAsStringAsync();
    }
}
