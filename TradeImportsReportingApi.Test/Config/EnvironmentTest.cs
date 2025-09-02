using Microsoft.AspNetCore.Builder;

namespace TradeImportsReportingApi.Test.Config;

public class EnvironmentTest
{

   [Fact]
   public void IsNotDevModeByDefault()
   { 
       var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
       var isDev = TradeImportsReportingApi.Config.Environment.IsDevMode(builder);
       Assert.False(isDev);
   }
}
