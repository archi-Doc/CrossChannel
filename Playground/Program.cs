using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CrossChannel;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace Playground;

[RadioServiceInterface]
public interface ITestService : IRadioService
{
    void Test1();

    RadioResult<int> Test2();

    Task Test3();

    Task<RadioResult<int>> Test4();
}

public static class Loader
{
    [ModuleInitializer]
    public static void __InitializeCC__()
    {
        TestService.__InitializeCC__();

    }
}

public partial class TestService : ITestService
{
    [ModuleInitializer]
    internal static void __InitializeCC__()
    {
        RadioRegistry.Register(new(typeof(TestService.ABC), x => new TestService.ABCBroker()));
        // RadioRegistry.Register(new(typeof(TestService.ABC.ABC2), x => new TestService.ABCBroker()));
    }

    [RadioServiceInterface]
    private partial interface ABC : IRadioService
    {
        internal static void __InitializeCC__()
        {
        }

            [RadioServiceInterface]
        private interface ABC2 : IRadioService
        {
        }
    }

    public class ABCBroker : ABC
    {

    }

    void ITestService.Test1()
    {
        Console.WriteLine("Test1");
    }

    RadioResult<int> ITestService.Test2()
    {
        Console.WriteLine("Test2");
        return new(1);
    }

    async Task ITestService.Test3()
    {
        await Console.Out.WriteLineAsync("Test3");
    }

    async Task<RadioResult<int>> ITestService.Test4()
    {
        await Console.Out.WriteLineAsync("Test4");
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

    public void Test1()
    {
        (var array, var countHint) = this.channel.InternalGetList().GetValuesAndCountHint();
        foreach (var x in array)
        {
            if (x is null) continue;
            if (!x.TryGetInstance(out var instance)) { x.Dispose(); continue; }
            instance.Test1();
        }
    }

    public RadioResult<int> Test2()
    {
        (var array, var countHint) = this.channel.InternalGetList().GetValuesAndCountHint();
        int firstResult = default;
        int[]? results = default;
        var count = 0;
        foreach (var x in array)
        {
            if (x is null) continue;
            if (!x.TryGetInstance(out var instance)) { x.Dispose(); continue; }

            if (instance.Test2().TryGetSingleResult(out var r))
            {
                if (count == 0)
                {
                    count = 1;
                    firstResult = r;
                }
                else
                {// count >= 1
                    if (results is null)
                    {// count == 1
                        results = new int[countHint];
                        results[0] = firstResult;
                    }

                    if (countHint < count) results[count++] = r;
                    else break;
                }
            }
        }

        if (count == 0) return default;
        else if (count == 1) return new(firstResult);
        else if (countHint != count) Array.Resize(ref results, count);
        return new(results!);
    }

    public async Task Test3()
    {
        (var array, var countHint) = this.channel.InternalGetList().GetValuesAndCountHint();
        var tasks = new Task[countHint];
        var count = 0;
        foreach (var x in array)
        {
            if (x is null) continue;
            if (!x.TryGetInstance(out var instance)) { x.Dispose(); continue; }
            if (countHint < count) tasks[count++] = instance.Test3();
            else break;
        }

        if (countHint != count) Array.Resize(ref tasks, count);
        if (count == 0) return;
        else if (count == 1) await tasks[0];
        else await Task.WhenAll(tasks);
    }

    public async Task<RadioResult<int>> Test4()
    {
        (var array, var countHint) = this.channel.InternalGetList().GetValuesAndCountHint();
        var tasks = new Task<RadioResult<int>>[countHint];
        var count = 0;
        foreach (var x in array)
        {
            if (x is null) continue;
            if (!x.TryGetInstance(out var instance)) { x.Dispose(); continue; }
            if (countHint < count) tasks[count++] = instance.Test4();
            else break;
        }

        if (countHint != count) Array.Resize(ref tasks, count);
        if (count == 0) return default;
        else if (count == 1) return await tasks[0];
        else return new((await Task.WhenAll(tasks)).Select(x => x.TryGetSingleResult(out var r) ? r : default).ToArray());
    }
}

class Program
{
    static void Main(string[] args)
    {
        var c = NewRadio.Open<ITestService>(default!);
        c.Close();
        NewRadio.Send<ITestService>().Test2();

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
