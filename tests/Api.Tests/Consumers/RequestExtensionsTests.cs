using AutoFixture;
using Defra.TradeImportsReportingApi.Api.Consumers;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.TestFixtures;

namespace Defra.TradeImportsReportingApi.Api.Tests.Consumers;

public class RequestExtensionsTests
{
    [Fact]
    public void ToRequest_WhenMessageSentAtIsNotUtc_ShouldThrow()
    {
        var request = RequestFixtures.ClearanceRequestFixture(messageSentAt: DateTime.Now).Create();

        var act = () => request.ToRequest("mrn");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task ToRequest_MapAsExpected()
    {
        var request = RequestFixtures
            .ClearanceRequestFixture(messageSentAt: new DateTime(2025, 7, 3, 13, 42, 0, DateTimeKind.Utc))
            .Create();

        var subject = request.ToRequest("mrn");

        await Verify(subject).ScrubMember(nameof(Request.Id)).DontScrubDateTimes();

        subject.Id.Should().NotBeEmpty();
    }
}
