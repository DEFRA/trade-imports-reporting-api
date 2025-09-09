using AutoFixture;
using Defra.TradeImportsReportingApi.Api.Consumers;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.TestFixtures;

namespace Defra.TradeImportsReportingApi.Api.Tests.Consumers;

public class FinalisationExtensionsTests
{
    [Fact]
    public void ToFinalisation_WhenMessageSentAtIsNotUtc_ShouldThrow()
    {
        var finalisation = FinalisationFixtures.FinalisationFixture(messageSentAt: DateTime.Now).Create();

        var act = () => finalisation.ToFinalisation("mrn");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(false, "0", ReleaseType.Automatic)]
    [InlineData(false, "1", ReleaseType.Cancelled)]
    [InlineData(false, "2", ReleaseType.Cancelled)]
    [InlineData(true, "0", ReleaseType.Manual)]
    [InlineData(true, "1", ReleaseType.Cancelled)]
    [InlineData(true, "2", ReleaseType.Cancelled)]
    public void ToFinalisation_ReleaseType_ShouldBeAsExpected(bool manualRelease, string finalState, string expected)
    {
        var finalisation = FinalisationFixtures
            .FinalisationFixture(finalState: finalState, isManualRelease: manualRelease)
            .Create();

        finalisation.ToFinalisation("mrn").ReleaseType.Should().Be(expected);
    }

    [Fact]
    public async Task ToFinalisation_MapAsExpected()
    {
        var finalisation = FinalisationFixtures
            .FinalisationFixture(
                finalState: "1",
                isManualRelease: false,
                messageSentAt: new DateTime(2025, 7, 3, 13, 42, 0, DateTimeKind.Utc)
            )
            .Create();

        var subject = finalisation.ToFinalisation("mrn");

        await Verify(subject).ScrubMember(nameof(Finalisation.Id)).DontScrubDateTimes();

        subject.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(ReleaseType.Unknown, false)]
    [InlineData(ReleaseType.Automatic, true)]
    [InlineData(ReleaseType.Manual, true)]
    [InlineData(ReleaseType.Cancelled, true)]
    public void ShouldBeStored_AsExpected(string releaseType, bool shouldStore)
    {
        var finalisation = FinalisationEntityFixtures
            .FinalisationFixture()
            .With(x => x.ReleaseType, releaseType)
            .Create();

        finalisation.ShouldBeStored().Should().Be(shouldStore);
    }
}
