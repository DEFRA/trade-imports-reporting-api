using System.Diagnostics;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Helpers;

public static class AsyncWaiter
{
    private static readonly TimeSpan s_defaultDelay = TimeSpan.FromSeconds(2);

    public static async Task<bool> WaitForAsync(Func<Task<bool>> condition, double? timeout = null)
    {
        var timer = Stopwatch.StartNew();
        var timeoutTimespan = TimeSpan.FromSeconds(timeout ?? 30);

        while (true)
        {
            if (await condition())
                return true;

            if (timer.Elapsed > timeoutTimespan)
                return false;

            await Task.Delay(s_defaultDelay);
        }
    }
}
