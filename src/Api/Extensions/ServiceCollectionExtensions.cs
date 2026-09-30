using Amazon.SQS;
using Defra.TradeImports.SQS.Endpoints;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Configuration;
using Defra.TradeImportsReportingApi.Api.Consumers;
using Defra.TradeImportsReportingApi.Api.Metrics;
using Defra.TradeImportsReportingApi.Api.Utils;
using Defra.TradeImportsReportingApi.Api.Utils.CorrelationId;
using Defra.TradeImportsReportingApi.Api.Utils.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SlimMessageBus;
using SlimMessageBus.Host;
using SlimMessageBus.Host.AmazonSQS;
using SlimMessageBus.Host.Interceptor;
using SlimMessageBus.Host.Serialization;
using SlimMessageBus.Host.Serialization.SystemTextJson;

namespace Defra.TradeImportsReportingApi.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReportingApiConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDeadLetterQueueManagementServices();
        services.AddSingleton<ICorrelationIdGenerator, CorrelationIdGenerator>();
        services.AddOptions<CdpOptions>().Bind(configuration).ValidateDataAnnotations();
        services.AddAWSService<IAmazonSQS>();
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

        var tracesChedsResourceEventsConsumerOptions = services
            .AddValidateOptions<TracesChedsResourceEventsConsumerOptions>(
                TracesChedsResourceEventsConsumerOptions.SectionName
            )
            .Get();

        var activityEventsConsumerOptions = services
            .AddValidateOptions<ActivityEventsConsumerOptions>(ActivityEventsConsumerOptions.SectionName)
            .Get();

        // The order of interceptors is important here!
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(TraceContextInterceptor<>));
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(LoggingInterceptor<>));
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(ConsumerMetricsInterceptor<>));

        services.AddTransient<ResourceEventsConsumer>();
        services.AddTransient<TracesChedConsumer>();
        services.AddTransient<BtmsToCdsActivityConsumer>();

        services.AddSlimMessageBus(smb =>
        {
            AddStringConsumerBus<ResourceEventsConsumer>(
                smb,
                "SQS_ResourceEvents",
                resourceEventsConsumerOptions.AutoStartConsumers,
                resourceEventsConsumerOptions.QueueName,
                resourceEventsConsumerOptions.ConsumersPerHost,
                configuration
            );

            if (activityEventsConsumerOptions.AutoStartConsumers)
            {
                smb.AddChildBus(
                    "SQS_ActivityEvents",
                    mbb =>
                    {
                        UseSqsProvider(mbb, configuration);

                        mbb.AddJsonSerializer();

                        mbb.WithSerializer<JsonMessageSerializer>();

                        mbb.AutoStartConsumersEnabled(activityEventsConsumerOptions.AutoStartConsumers)
                            .Consume<BtmsActivityEvent<BtmsToCdsActivity>>(x =>
                                x.WithConsumer<BtmsToCdsActivityConsumer>()
                                    .Queue(activityEventsConsumerOptions.QueueName)
                                    .Instances(activityEventsConsumerOptions.ConsumersPerHost)
                            );
                    }
                );
            }

            AddStringConsumerBus<TracesChedConsumer>(
                smb,
                "SQS_ResourceEvents_TracesCheds",
                tracesChedsResourceEventsConsumerOptions.AutoStartConsumers,
                tracesChedsResourceEventsConsumerOptions.QueueName,
                tracesChedsResourceEventsConsumerOptions.ConsumersPerHost,
                configuration
            );
        });

        return services;
    }

    private static void AddStringConsumerBus<TConsumer>(
        MessageBusBuilder smb,
        string busName,
        bool autoStartConsumers,
        string queueName,
        int consumersPerHost,
        IConfiguration configuration
    )
        where TConsumer : class, IConsumer<string>
    {
        if (!autoStartConsumers)
            return;

        smb.AddChildBus(
            busName,
            mbb =>
            {
                UseSqsProvider(mbb, configuration);

                mbb.RegisterSerializer<ToStringSerializer>(s =>
                {
                    s.TryAddSingleton(_ => new ToStringSerializer());
                    s.TryAddSingleton<IMessageSerializer<string>>(svp => svp.GetRequiredService<ToStringSerializer>());
                });

                mbb.WithSerializer<ToStringSerializer>();

                mbb.AutoStartConsumersEnabled(autoStartConsumers)
                    .Consume<string>(x => x.WithConsumer<TConsumer>().Queue(queueName).Instances(consumersPerHost));
            }
        );
    }

    private static void UseSqsProvider(MessageBusBuilder mbb, IConfiguration configuration) =>
        mbb.WithProviderAmazonSQS(cfg =>
        {
            cfg.TopologyProvisioning.Enabled = false;
            cfg.SqsClientProviderFactory = _ => new CdpCredentialsSqsClientProvider(cfg.SqsClientConfig, configuration);
        });
}
