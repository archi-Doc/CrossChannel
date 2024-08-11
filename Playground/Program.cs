using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Arc.WeakDelegate;
using CrossChannel;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace Playground;

public interface ITestService : IRadioService
{
    RadioResult<int> Test();
}

class Program
{
    static void Main(string[] args)
    {
        NewRadio.Open<ITestService>(default!);
        NewRadio.Send<ITestService>().Test();

        Console.WriteLine("Hello World!");

        var sc = new ServiceCollection();
        sc.AddMessagePipe();
        var provider = sc.BuildServiceProvider();

        var channel = Radio.Open<int>(x => Console.WriteLine($"CrossChannel: {x}"));

        var sub = provider.GetService<ISubscriber<int>>()!;
        var pub = provider.GetService<IPublisher<int>>()!;
        var subscribe = sub.Subscribe(x => Console.WriteLine($"MessagePipe: {x}"));

        var taskCrossChannel = Task.Run(async () =>
        {
            for (var i = 0; i < 10; i++)
            {
                Radio.Send<int>(i);
                pub.Publish(i);
                await Task.Delay(1000);
            }
        });

        Task.Delay(1000).Wait();

        var taskAdd = Task.Run(async () =>
        {
            for (var i = 0; i < 10; i++)
            {
                Radio.Open<int>(x => Console.WriteLine($"CC{i}: {x}"));
                sub.Subscribe(x => Console.WriteLine($"MP{i}: {x}"));
                await Task.Delay(1000);
            }
            /*using (var c = Radio.Open<int>(x => Console.WriteLine($"CC: {x}")))
            using (var s = sub.Subscribe(x => Console.WriteLine($"MP: {x}")))
            {
                await Task.Delay(3000);
            }*/
        });

        taskCrossChannel.Wait();
        subscribe.Dispose();
        channel.Dispose();
    }
}
