using Defra.TradeImportsReportingApi.Api.Authentication;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Data.Extensions;
using Defra.TradeImportsReportingApi.Api.Endpoints;
using Defra.TradeImportsReportingApi.Api.Endpoints.Admin;
using Defra.TradeImportsReportingApi.Api.Extensions;
using Defra.TradeImportsReportingApi.Api.Health;
using Defra.TradeImportsReportingApi.Api.Metrics;
using Defra.TradeImportsReportingApi.Api.OpenApi;
using Defra.TradeImportsReportingApi.Api.Utils;
using Defra.TradeImportsReportingApi.Api.Utils.Http;
using Defra.TradeImportsReportingApi.Api.Utils.Logging;
using Elastic.CommonSchema.Serilog;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console(new EcsTextFormatter()).CreateBootstrapLogger();

try
{
    var app = CreateWebApplication(args);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    await Log.CloseAndFlushAsync();
}

return;

static WebApplication CreateWebApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    ConfigureWebApplication(builder, args);

    return BuildWebApplication(builder);
}

static void ConfigureWebApplication(WebApplicationBuilder builder, string[] args)
{
    var integrationTest = args.Contains("--integrationTest=true");

    builder.Configuration.AddJsonFile(
        $"appsettings.cdp.{Environment.GetEnvironmentVariable("ENVIRONMENT")?.ToLower()}.json",
        integrationTest
    );
    builder.Configuration.AddEnvironmentVariables();

    // Load certificates into Trust Store - Note must happen before Mongo and Http client connections
    builder.Services.AddCustomTrustStore();

    builder.ConfigureLoggingAndTracing(integrationTest);

    builder.Services.AddAuthenticationAuthorization();
    builder.Services.Configure<RouteHandlerOptions>(o =>
    {
        // Without this, bad request detail will only be thrown in DEVELOPMENT mode
        o.ThrowOnBadRequest = true;
    });
    builder.Services.AddProblemDetails();
    builder.Services.AddHealth(builder.Configuration);
    builder.Services.AddOpenApi(builder.Configuration);
    builder.Services.AddReportingApiConfiguration(builder.Configuration);

    builder.Services.AddHttpProxyClient();

    builder.Services.AddConsumers(builder.Configuration);
    builder.Services.AddCustomMetrics();

    builder.Services.AddDbContext(builder.Configuration, integrationTest);
    builder.Services.AddTransient<IReportRepository, ReportRepository>();
}

static WebApplication BuildWebApplication(WebApplicationBuilder builder)
{
    var app = builder.Build();

    app.UseEmfExporter();
    app.UseHeaderPropagation();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<MetricsMiddleware>();
    app.MapHealth();
    app.UseStatusCodePages();
    app.MapReleasesEndpoints();
    app.MapMatchesEndpoints();
    app.MapClearanceRequestEndpoints();
    app.MapNotificationEndpoints();
    app.MapGeneralEndpoints();
    app.MapAdminEndpoints();
    app.UseOpenApi();
    app.UseExceptionHandler(
        new ExceptionHandlerOptions
        {
            AllowStatusCode404Response = true,
            ExceptionHandler = async context =>
            {
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                var error = exceptionHandlerFeature?.Error;
                string? detail = null;

                if (error is BadHttpRequestException badHttpRequestException)
                {
                    context.Response.StatusCode = badHttpRequestException.StatusCode;
                    detail = badHttpRequestException.Message;
                }

                await context
                    .RequestServices.GetRequiredService<IProblemDetailsService>()
                    .WriteAsync(
                        new ProblemDetailsContext
                        {
                            HttpContext = context,
                            AdditionalMetadata = exceptionHandlerFeature?.Endpoint?.Metadata,
                            ProblemDetails = { Status = context.Response.StatusCode, Detail = detail },
                        }
                    );
            },
        }
    );

    return app;
}

#pragma warning disable S2094
namespace Defra.TradeImportsReportingApi.Api
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program;
}
#pragma warning restore S2094
