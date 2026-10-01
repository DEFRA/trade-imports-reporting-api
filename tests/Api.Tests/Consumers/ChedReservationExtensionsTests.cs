using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Traces;
using Defra.TradeImportsReportingApi.Api.Consumers;
using Defra.TradeImportsReportingApi.Api.Data.Entities;

namespace Defra.TradeImportsReportingApi.Api.Tests.Consumers;

public class ChedReservationExtensionsTests
{
    private static readonly DateTime s_activity = new(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

    private static ResourceEvent<ChedReservationEvent> CreateResourceEvent(string operation, Reservation? reservation) =>
        new()
        {
            ResourceId = "ched1_mrn1",
            ResourceType = ResourceEventResourceTypes.ChedReservation,
            Operation = operation,
            Timestamp = s_activity,
            Resource = reservation is null ? null : new ChedReservationEvent { Id = "test", Reservation = reservation },
        };

    private static Reservation CreateReservation() =>
        new()
        {
            ChedId = "ched1",
            Mrn = "mrn1",
            Status = "Reserved",
            Timestamp = new DateTime(2025, 9, 3, 16, 0, 0, DateTimeKind.Utc),
            Commodities = [],
        };

    [Fact]
    public async Task ToChedReservation_WhenCreated_MapAsExpected()
    {
        var subject = CreateResourceEvent(ResourceEventOperations.Created, CreateReservation()).ToChedReservation();

        await Verify(subject).ScrubMember(nameof(ChedReservation.Id)).DontScrubDateTimes();

        subject.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void ToChedReservation_WhenDeleted_ShouldNotPopulatePayloadFields()
    {
        var subject = CreateResourceEvent(ResourceEventOperations.Deleted, null).ToChedReservation();

        subject.Id.Should().NotBeEmpty();
        subject.Timestamp.Should().Be(s_activity);
        subject.ResourceId.Should().Be("ched1_mrn1");
        subject.Operation.Should().Be(ResourceEventOperations.Deleted);
        subject.ChedId.Should().BeNull();
        subject.Mrn.Should().BeNull();
        subject.Status.Should().BeNull();
    }
}
