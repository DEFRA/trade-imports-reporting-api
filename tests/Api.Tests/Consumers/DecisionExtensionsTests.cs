using AutoFixture;
using Defra.TradeImportsReportingApi.Api.Consumers;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.TestFixtures;

namespace Defra.TradeImportsReportingApi.Api.Tests.Consumers;

public class DecisionExtensionsTests
{
    [Fact]
    public void ToDecision_WhenMatch_ShouldBeTrue()
    {
        var decision = DecisionFixtures.ClearanceDecisionFixture().Create();

        foreach (var item in decision.Items)
        {
            foreach (var check in item.Checks)
            {
                check.DecisionCode = $"Not{DecisionCode.NoMatch}";
            }
        }

        decision.ToDecision("mrn", DateTime.UtcNow).Match.Should().BeTrue();
    }

    [Fact]
    public void ToDecision_WhenSingleNoMatch_ShouldBeFalse()
    {
        var decision = DecisionFixtures.ClearanceDecisionFixture().Create();

        decision.Items[0].Checks[0].DecisionCode = DecisionCode.NoMatch;

        decision.ToDecision("mrn", DateTime.UtcNow).Match.Should().BeFalse();
    }

    [Fact]
    public async Task ToDecision_MapAsExpected()
    {
        var decisionCreated = new DateTime(2025, 9, 4, 13, 56, 0, DateTimeKind.Utc);
        var decision = DecisionFixtures.ClearanceDecisionFixture().With(x => x.Created, decisionCreated).Create();

        decision.Items[0].Checks[0].DecisionCode = DecisionCode.NoMatch;

        var mrnCreated = new DateTime(2025, 9, 4, 13, 55, 0, DateTimeKind.Utc);
        var subject = decision.ToDecision("mrn", mrnCreated);

        await Verify(subject).ScrubMember(nameof(Decision.Id)).DontScrubDateTimes();

        subject.Id.Should().NotBeEmpty();
    }
}
