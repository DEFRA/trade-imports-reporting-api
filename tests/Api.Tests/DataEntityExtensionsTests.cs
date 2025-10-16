using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Data.Extensions;

namespace Defra.TradeImportsReportingApi.Api.Tests;

public class DataEntityExtensionsTests
{
    [Fact]
    public void NoAttributeTest()
    {
        typeof(NoAttributeClassEntity).DataEntityName().Should().Be("NoAttributeClass");
    }

    [Fact]
    public void AttributeTest()
    {
        typeof(AttributeClassEntity).DataEntityName().Should().Be("TestName");
    }

    public record NoAttributeClassEntity(string Test);

    [DbCollection("TestName")]
    public record AttributeClassEntity(string Test);
}
