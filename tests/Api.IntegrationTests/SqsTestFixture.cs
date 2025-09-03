namespace Defra.TradeImportsReportingApi.Api.IntegrationTests;

// ReSharper disable once ClassNeverInstantiated.Global
public class SqsTestFixture : IAsyncLifetime
{
    private SqsQueueClient? _resourceEventsQueue;

    public SqsQueueClient ResourceEventsQueue => _resourceEventsQueue!;

    public Task InitializeAsync()
    {
        _resourceEventsQueue = new SqsQueueClient("trade_imports_data_upserted_reporting_api");

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _resourceEventsQueue?.Dispose();

        return Task.CompletedTask;
    }
}

[Collection("UsesSqs")]
public class SqsTestBase : IntegrationTestBase;

[CollectionDefinition("UsesSqs")]
public class SqsTestFixtureCollection : ICollectionFixture<SqsTestFixture>;
