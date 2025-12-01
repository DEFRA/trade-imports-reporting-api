using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsReportingApi.Api.Configuration;
using Microsoft.AspNetCore.Authentication;

namespace Defra.TradeImportsReportingApi.Api.Authentication;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationAuthorization(this IServiceCollection services)
    {
        services.AddOptions<AclOptions>().BindConfiguration("Acl");

        services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
                BasicAuthenticationHandler.SchemeName,
                _ => { }
            );

        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                PolicyNames.Execute,
                builder => builder.RequireAuthenticatedUser().RequireClaim(Claims.Scope, Scopes.Execute)
            );

        return services;
    }
}
