using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Extensions;

namespace Defra.TradeImportsReportingApi.Api.Tests.Consumers;

public class ClearanceDecisionResultExtensionsTests
{
    private const string ValidDecisionCode = "C03";

    public static TheoryData<string> KnownNoMatchCodes =>
        ["E20", "E30", "E31", "E70", "E71", "E72", "E73", "E75", "E82", "E83", "E87", "E99"];

    public static TheoryData<string> KnownMatchCodes =>
        ["E74", "E80", "E84", "E85", "E86", "E88", "E90", "E92", "E93", "E94", "E95", "E96", "E97"];

    [Fact]
    public void DecisionIsAMatch_WhenNull_ReturnsFalse()
    {
        ClearanceDecisionResult? result = null;

        result.DecisionIsAMatch().Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(DecisionCode.NoMatch)]
    public void DecisionIsAMatch_WhenDecisionCodeIsInvalidOrNullOrNoMatch_ReturnsFalse(string? decisionCode)
    {
        var result = new ClearanceDecisionResult { DecisionCode = decisionCode };

        result.DecisionIsAMatch().Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("X99")]
    public void DecisionIsAMatch_WhenDecisionCodeIsNotNoMatch_AndInternalDecisionCodeIsNullOrWhitespaceOrUnknown_ReturnsTrue(
        string? internalDecisionCode
    )
    {
        var result = new ClearanceDecisionResult
        {
            DecisionCode = ValidDecisionCode,
            InternalDecisionCode = internalDecisionCode,
        };

        result.DecisionIsAMatch().Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(KnownNoMatchCodes))]
    public void DecisionIsAMatch_WhenDecisionCodeIsNotNoMatch_InternalDecisionCodeIsAKnownNoMatchCode_ReturnsFalse(
        string code
    )
    {
        var result = new ClearanceDecisionResult { DecisionCode = ValidDecisionCode, InternalDecisionCode = code };

        result.DecisionIsAMatch().Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(KnownMatchCodes))]
    public void DecisionIsAMatch_WhenDecisionCodeIsMatch_AndKnownMatchCode_ReturnsTrue(string code)
    {
        var result = new ClearanceDecisionResult { DecisionCode = ValidDecisionCode, InternalDecisionCode = code };

        result.DecisionIsAMatch().Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(KnownNoMatchCodes))]
    [MemberData(nameof(KnownMatchCodes))]
    public void InternalDecisionCodeIsUnknown_WhenKnownCode_ReturnsFalse(string code)
    {
        var result = new ClearanceDecisionResult { InternalDecisionCode = code };

        result.InternalDecisionCodeIsUnknown().Should().BeFalse();
    }

    [Fact]
    public void InternalDecisionCodeIsUnknown_WhenUnknownCode_ReturnsTrue()
    {
        var result = new ClearanceDecisionResult { InternalDecisionCode = "X99" };

        result.InternalDecisionCodeIsUnknown().Should().BeTrue();
    }
}
