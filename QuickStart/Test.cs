using System;
using System.Threading;
using System.Threading.Tasks;
using CrossChannel;

namespace QuickStart;

[RadioServiceInterface] // Add the RadioServiceInterface attribute.
public interface IDelayService : IRadioService
{
    Task Delay(int milliseconds, CancellationToken cancellationToken);
}

public class DelayService : IDelayService
{// Implement the interface.
    async Task IDelayService.Delay(int milliseconds, CancellationToken cancellationToken)
    {
        Console.WriteLine("Start");
        await Task.Delay(milliseconds, cancellationToken);
        Console.WriteLine("End");
    }
}

internal static class Test
{
    public static async Task Test1()
    {
        var cts = new CancellationTokenSource();
        using var channel = Radio.Open((IDelayService)new DelayService());

        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            cts.Cancel();
        });

        try
        {
            await Radio.Send<IDelayService>().Delay(1000, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Canceled");
        }
    }
}
