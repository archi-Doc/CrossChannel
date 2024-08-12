using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

public class TestService : ITestService
{
    RadioResult<int> ITestService.Test()
    {
        Console.WriteLine("TestServiceBroker");
        return new(1);
    }
}

public class TestServiceBroker : ITestService
{
    private readonly Channel<ITestService> channel;

    public TestServiceBroker(Channel<ITestService> channel)
    {
        this.channel = channel;
    }

    public RadioResult<int> Test()
    {
        this.channel.EnterScope();
        try
        {
            var list = this.channel.InternalGetList();
            var array = list.GetValues();
            if (list.Count == 1)
            {
                foreach (var x in array)
                {
                    if (x is not null)
                    {
                        if (x.TryGetInstance(out var instance))
                        {
                            return instance.Test();
                        }
                        else
                        {
                            x.InternalDispose();
                        }
                    }
                }
            }
            else if (list.Count > 1)
            {
                var results = new int[list.Count];
                var count = 0;
                foreach (var x in array)
                {
                    if (x is not null)
                    {
                        if (x.TryGetInstance(out var instance))
                        {
                            if (instance.Test().TryGetSingleResult(out var r))
                            {
                                results[count++] = r;
                            }
                        }
                        else
                        {
                            x.InternalDispose();
                        }
                    }
                }

                if (results.Length != count)
                {
                    Array.Resize(ref results, count);
                }

                return new(results);
            }
        }
        finally
        {
            this.channel.ExitScope();
        }

        return default;
    }
}

class Program
{
    static void Main(string[] args)
    {
        var c = NewRadio.Open<ITestService>(default!);
        c.Close();
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
