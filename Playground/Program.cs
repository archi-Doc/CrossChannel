using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrossChannel;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace Playground;

public interface ITestService : IRadioService
{
    RadioResult<int> Test();

    void Test2();

    Task<RadioResult<int>> Test3();
}

public class TestService : ITestService
{
    RadioResult<int> ITestService.Test()
    {
        Console.WriteLine("Test");
        return new(1);
    }

    void ITestService.Test2()
    {
        Console.WriteLine("Test2");
    }

    async Task<RadioResult<int>> ITestService.Test3()
    {
        await Console.Out.WriteLineAsync("Test3");
        return new(3);
    }
}

public class TestServiceBroker : ITestService
{
    private readonly Channel<ITestService> channel;

    public TestServiceBroker(object channel)
    {
        this.channel = (Channel<ITestService>)channel;
    }

    public RadioResult<int> Test()
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
                        x.Dispose();
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
                        try
                        {
                            if (instance.Test().TryGetSingleResult(out var r))
                            {
                                results[count++] = r;
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        x.Dispose();
                    }
                }
            }

            if (results.Length != count)
            {
                Array.Resize(ref results, count);
            }

            return new(results);
        }

        return default;
    }

    public void Test2()
    {
        var array = this.channel.InternalGetList().GetValues();
        foreach (var x in array)
        {
            if (x is not null)
            {
                if (x.TryGetInstance(out var instance))
                {
                    try { instance.Test2(); }
                    catch { }
                }
                else x.Dispose();
            }
        }
    }

    public async Task<RadioResult<int>> Test3()
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
                        try
                        {
                            return await instance.Test3().ConfigureAwait(false);
                        }
                        catch { }
                    }
                    else x.Dispose();
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
                        try
                        {
                            if ((await instance.Test3().ConfigureAwait(false)).TryGetSingleResult(out var r)) results[count++] = r;
                        }
                        catch { }
                    }
                    else x.Dispose();
                }
            }

            if (results.Length != count)
            {
                Array.Resize(ref results, count);
            }

            return new(results);
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
