using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Testing;
using MongoDB.Driver;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios;

public class FinalisationTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Fact]
    public async Task WhenSingleFinalisation_ShouldBeSingleCount()
    {
        var mrn = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclaration
            {
                Finalisation = new Finalisation
                {
                    ExternalVersion = 1,
                    FinalState = "0",
                    IsManualRelease = false,
                    MessageSentAt = messageSentAt,
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );

        await SendMessage(resourceEvent, CreateMessageAttributes(resourceEvent));

        await WaitForMrn(mrn);

        var client = CreateHttpClient();

        var response = await client.GetAsync(
            Testing.Endpoints.ReleasesSummary.Get(
                EndpointQuery
                    .New.Where(EndpointFilter.From(messageSentAt.AddHours(-1)))
                    .Where(EndpointFilter.To(messageSentAt.AddHours(1)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }

    [Fact]
    public async Task WhenMultipleFinalisationForSameMrn_ShouldBeSingleCount()
    {
        var mrn = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclaration
            {
                Finalisation = new Finalisation
                {
                    ExternalVersion = 1,
                    FinalState = "0",
                    IsManualRelease = false,
                    MessageSentAt = messageSentAt,
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );
        var resourceEvent2 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclaration
            {
                Finalisation = new Finalisation
                {
                    ExternalVersion = 1,
                    FinalState = "0",
                    IsManualRelease = false,
                    MessageSentAt = messageSentAt.AddMinutes(5), // Different message sent at
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));

        await WaitForMrn(mrn, count: 2);

        var client = CreateHttpClient();

        var response = await client.GetAsync(
            Testing.Endpoints.ReleasesSummary.Get(
                EndpointQuery
                    .New.Where(EndpointFilter.From(messageSentAt.AddHours(-1)))
                    .Where(EndpointFilter.To(messageSentAt.AddHours(1)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }

    [Fact]
    public async Task WhenMultipleFinalisationForSameMrn_AndChangeFromAutomaticToManual_ShouldBeSingleCount()
    {
        var mrn = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclaration
            {
                Finalisation = new Finalisation
                {
                    ExternalVersion = 1,
                    FinalState = "0",
                    IsManualRelease = false,
                    MessageSentAt = messageSentAt,
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );
        var resourceEvent2 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclaration
            {
                Finalisation = new Finalisation
                {
                    ExternalVersion = 1,
                    FinalState = "0",
                    IsManualRelease = true, // Change from Automatic to Manual
                    MessageSentAt = messageSentAt.AddMinutes(5), // Different message sent at
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));

        await WaitForMrn(mrn, count: 2);

        var client = CreateHttpClient();

        var response = await client.GetAsync(
            Testing.Endpoints.ReleasesSummary.Get(
                EndpointQuery
                    .New.Where(EndpointFilter.From(messageSentAt.AddHours(-1)))
                    .Where(EndpointFilter.To(messageSentAt.AddHours(1)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }

    [Fact]
    public async Task WhenMultipleFinalisationForDifferentMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount()
    {
        var mrn1 = Guid.NewGuid().ToString();
        var mrn2 = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            mrn1,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclaration
            {
                Finalisation = new Finalisation
                {
                    ExternalVersion = 1,
                    FinalState = "0",
                    IsManualRelease = false,
                    MessageSentAt = messageSentAt,
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );
        var resourceEvent2 = CreateResourceEvent(
            mrn2,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclaration
            {
                Finalisation = new Finalisation
                {
                    ExternalVersion = 1,
                    FinalState = "0",
                    IsManualRelease = false,
                    MessageSentAt = messageSentAt.AddHours(2), // Outside From and To
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));

        await WaitForMrn(mrn1);
        await WaitForMrn(mrn2);

        var client = CreateHttpClient();

        var response = await client.GetAsync(
            Testing.Endpoints.ReleasesSummary.Get(
                EndpointQuery
                    .New.Where(EndpointFilter.From(messageSentAt.AddHours(-1)))
                    .Where(EndpointFilter.To(messageSentAt.AddHours(1)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }

    private async Task WaitForMrn(string mrn, int count = 1)
    {
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
            {
                var finalisation = await Finalisations.FindAsync(
                    Builders<Data.Entities.Finalisation>.Filter.Eq(x => x.Mrn, mrn)
                );

                return (await finalisation.ToListAsync()).Count == count;
            })
        );
    }
}
