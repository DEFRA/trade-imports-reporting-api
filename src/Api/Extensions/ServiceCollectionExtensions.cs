using Defra.TradeImportsReportingApi.Api.Configuration;
using Defra.TradeImportsReportingApi.Api.Consumers;
using Defra.TradeImportsReportingApi.Api.Metrics;
using Defra.TradeImportsReportingApi.Api.Utils;
using Defra.TradeImportsReportingApi.Api.Utils.CorrelationId;
using Defra.TradeImportsReportingApi.Api.Utils.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SlimMessageBus.Host;
using SlimMessageBus.Host.AmazonSQS;
using SlimMessageBus.Host.Interceptor;
using SlimMessageBus.Host.Serialization;

namespace Defra.TradeImportsReportingApi.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReportingApiConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSingleton<ICorrelationIdGenerator, CorrelationIdGenerator>();
        services.AddOptions<CdpOptions>().Bind(configuration).ValidateDataAnnotations();

        return services;
    }

    public static IServiceCollection AddCustomMetrics(this IServiceCollection services)
    {
        services.AddTransient<MetricsMiddleware>();

        services.AddSingleton<IConsumerMetrics, ConsumerMetrics>();
        services.AddSingleton<RequestMetrics>();

        return services;
    }

    public static IServiceCollection AddConsumers(this IServiceCollection services, IConfiguration configuration)
    {
        var resourceEventsConsumerOptions = services
            .AddValidateOptions<ResourceEventsConsumerOptions>(ResourceEventsConsumerOptions.SectionName)
            .Get();

        // The order of interceptors is important here!
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(TraceContextInterceptor<>));
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(LoggingInterceptor<>));
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(ConsumerMetricsInterceptor<>));

        services.AddTransient<ResourceEventsConsumer>();

        services.AddSlimMessageBus(smb =>
        {
            if (resourceEventsConsumerOptions.AutoStartConsumers)
            {
                smb.AddChildBus(
                    "SQS_ResourceEvents",
                    mbb =>
                    {
                        mbb.WithProviderAmazonSQS(cfg =>
                        {
                            cfg.TopologyProvisioning.Enabled = false;
                            cfg.SqsClientProviderFactory = _ => new CdpCredentialsSqsClientProvider(
                                cfg.SqsClientConfig,
                                configuration
                            );
                        });

                        mbb.RegisterSerializer<ToStringSerializer>(s =>
                        {
                            s.TryAddSingleton(_ => new ToStringSerializer());
                            s.TryAddSingleton<IMessageSerializer<string>>(svp =>
                                svp.GetRequiredService<ToStringSerializer>()
                            );
                        });

                        mbb.WithSerializer<ToStringSerializer>();

                        mbb.AutoStartConsumersEnabled(resourceEventsConsumerOptions.AutoStartConsumers)
                            .Consume<string>(x =>
                                x.WithConsumer<ResourceEventsConsumer>()
                                    .Queue(resourceEventsConsumerOptions.QueueName)
                                    .Instances(resourceEventsConsumerOptions.ConsumersPerHost)
                            );
                    }
                );
            }
        });

        return services;
    }
}
