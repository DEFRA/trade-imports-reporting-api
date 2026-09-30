using AutoFixture;
using Defra.TradeImportsReportingApi.Api.Consumers;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Models;
using Defra.TradeImportsReportingApi.TestFixtures;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsReportingApi.Api.Tests.Consumers;

public class TracesChedExtensionsTests
{
    private static DefraUNVTDCHEDProfile CreateChed(string identifier) =>
        new()
        {
            ExchangedDocument = new ExchangedDocument { Identifier = identifier },
            SpecifiedConsignment = new Consignment(),
        };

    [Theory]
    [InlineData("CHEDA.GB.2025.1234567", NotificationType.ChedA)]
    [InlineData("CHEDP.GB.2025.1234567", NotificationType.ChedP)]
    [InlineData("CHEDPP.GB.2025.1234567", NotificationType.ChedPP)]
    [InlineData("CHEDD.GB.2025.1234567", NotificationType.ChedD)]
    [InlineData("IMP.GB.2025.1234567", NotificationType.Unknown)]
    [InlineData("CHEDP", NotificationType.ChedP)]
    [InlineData("", NotificationType.Unknown)]
    public void ToTracesChed_NotificationType_ShouldBeAsExpected(string identifier, string expected)
    {
        CreateChed(identifier)
            .ToTracesChed("ched", DateTime.UtcNow, DateTime.UtcNow)
            .NotificationType.Should()
            .Be(expected);
    }

    [Fact]
    public async Task ToTracesChed_MapAsExpected()
    {
        var subject = CreateChed("CHEDA.GB.2025.1234567")
            .ToTracesChed(
                "ched",
                new DateTime(2025, 7, 3, 13, 42, 0, DateTimeKind.Utc),
                new DateTime(2025, 7, 3, 14, 42, 0, DateTimeKind.Utc)
            );

        await Verify(subject).ScrubMember(nameof(TracesChed.Id)).DontScrubDateTimes();

        subject.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(NotificationType.Unknown, false)]
    [InlineData(NotificationType.ChedA, true)]
    [InlineData(NotificationType.ChedP, true)]
    [InlineData(NotificationType.ChedPP, true)]
    [InlineData(NotificationType.ChedD, true)]
    public void ShouldBeStored_AsExpected(string notificationType, bool shouldStore)
    {
        var tracesChed = TracesChedEntityFixtures
            .TracesChedFixture()
            .With(x => x.NotificationType, notificationType)
            .Create();

        tracesChed.ShouldBeStored().Should().Be(shouldStore);
    }
}
