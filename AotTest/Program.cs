// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

// A Native AOT smoke test: it exercises every part of CrossChannel that could rely on
// dynamic code generation, and fails with a non-zero exit code if anything misbehaves.
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CrossChannel;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0649 // Field is never assigned to (it is written through Unsafe.AsRef).

namespace AotTest;

[RadioService]
public interface ITestService : IRadioService
{
    void Notify(int value);

    Task NotifyAsync(int value);

    RadioResult<int> Double(int value);

    Task<RadioResult<int>> DoubleAsync(int value);
}

[RadioService(MaxLinks = 1)]
public interface ISingleService : IRadioService
{
    RadioResult<string> Name();
}

[RadioService(AutoRegisterServiceAndSender = false)]
public interface IManualService : IRadioService
{
    void Notify();
}

public readonly record struct ChannelKey(int Value);

public class TestService : ITestService
{
    public int Received { get; private set; }

    public void Notify(int value) => this.Received += value;

    public async Task NotifyAsync(int value)
    {
        await Task.Yield();
        this.Notify(value);
    }

    public RadioResult<int> Double(int value) => new(value * 2);

    public async Task<RadioResult<int>> DoubleAsync(int value)
    {
        await Task.Yield();
        return new(value * 2);
    }
}

public class SingleService : ISingleService
{
    public RadioResult<string> Name() => new("single");
}

public class CopyBase
{
    private readonly Guid id;

    public CopyBase(Guid id) => this.id = id;

    public Guid Id => this.id;
}

public class CopyTarget : CopyBase
{
    public CopyTarget(Guid id = default)
        : base(id)
    {
    }

    public int Value { get; init; }

    public string Text = string.Empty;

    private readonly long id;

    private readonly byte[] payload = [];

    public void Prepare()
    {
        this.Text = "copied";
        Unsafe.AsRef<long>(in this.id) = 1234;
        Unsafe.AsRef<byte[]>(in this.payload) = [1, 2, 3];
    }

    public bool Compare(CopyTarget other)
        => this.Id == other.Id &&
        this.Value == other.Value &&
        this.Text == other.Text &&
        this.id == other.id &&
        this.payload.AsSpan().SequenceEqual(other.payload);
}

internal static class Program
{
    private static int failures;

    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"IsDynamicCodeSupported: {RuntimeFeature.IsDynamicCodeSupported}");
        Console.WriteLine($"IsDynamicCodeCompiled: {RuntimeFeature.IsDynamicCodeCompiled}");

        if (args.Contains("--require-no-dynamic-code"))
        {
            Check(!RuntimeFeature.IsDynamicCodeSupported && !RuntimeFeature.IsDynamicCodeCompiled, "Dynamic code is disabled");
        }

        await StaticRadio();
        await RadioInstance();
        KeyedChannels();
        DependencyInjection();
        FieldCopierRoundTrip();

        Console.WriteLine(failures == 0 ? "All compatibility checks passed." : $"{failures} compatibility check(s) FAILED.");
        return failures == 0 ? 0 : 1;
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
        {
            failures++;
            Console.WriteLine($"  FAILED: {name}");
        }
        else
        {
            Console.WriteLine($"  ok: {name}");
        }
    }

    private static async Task StaticRadio()
    {
        Console.WriteLine("Static Radio");
        var service = new TestService();
        using var link = Radio.Open<ITestService>(service);
        Check(link is not null, "Open");

        Radio.Send<ITestService>().Notify(5);
        Check(service.Received == 5, "Send void");
        Check(Radio.Send<ITestService>().Double(21).TryGetFirst(out var d) && d == 42, "Send result");

        await Radio.Send<ITestService>().NotifyAsync(3);
        Check(service.Received == 8, "Send async notification");

        var async = await Radio.Send<ITestService>().DoubleAsync(11);
        Check(async.TryGetFirst(out var a) && a == 22, "Send async result");
    }

    private static async Task RadioInstance()
    {
        Console.WriteLine("LocalRadio");
        var radio = new LocalRadio();
        var sender = radio.Send<ITestService>();
        sender.Notify(1);
        await sender.NotifyAsync(1);
        Check(sender.Double(1).IsEmpty && (await sender.DoubleAsync(1)).IsEmpty, "No subscribers");
        var first = new TestService();
        var second = new TestService();
        using var link1 = radio.Open<ITestService>(first);
        using var link2 = radio.Open<ITestService>(second);

        var results = radio.Send<ITestService>().Double(3);
        Check(results.Count == 2 && results.All(x => x == 6), "Aggregated results");

        var asyncResults = await radio.Send<ITestService>().DoubleAsync(4);
        Check(asyncResults.Count == 2 && asyncResults.All(x => x == 8), "Aggregated async results");

        await sender.NotifyAsync(7);
        Check(first.Received == 7 && second.Received == 7, "Aggregated async notifications");

        using var single = radio.Open<ISingleService>(new SingleService());
        Check(single is not null, "MaxLinks first link");
        Check(radio.Open<ISingleService>(new SingleService()) is null, "MaxLinks exceeded");
    }

    private static void KeyedChannels()
    {
        Console.WriteLine("Keyed channels");
        var radio = new LocalRadio();
        using var intLink = radio.OpenWithKey<ITestService, int>(new TestService(), 1);
        using var stringLink = radio.OpenWithKey<ITestService, string>(new TestService(), "1");

        Check(radio.SendWithKey<ITestService, int>(1).Double(2).TryGetFirst(out var i) && i == 4, "Int key");
        Check(radio.SendWithKey<ITestService, string>("1").Double(3).TryGetFirst(out var s) && s == 6, "String key");
        Check(radio.SendWithKey<ITestService, int>(2).Double(4).IsEmpty, "Missing key returns the empty channel");
    }

    private static void DependencyInjection()
    {
        Console.WriteLine("Dependency injection");
        foreach (var useLocalRadio in new[] { true, false })
        {
            using var provider = new ServiceCollection().AddCrossChannel(useLocalRadio).BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
            var channel = provider.GetRequiredService<IChannel<ITestService>>();
            using var link = channel.Open(new TestService());

            var sender = provider.GetRequiredService<ISender<ITestService>>();
            Check(sender.Send().Double(5).TryGetFirst(out var v) && v == 10, $"ISender (useLocalRadio: {useLocalRadio})");
            Check(sender.SendWithKey(9).Double(5).IsEmpty, $"ISender.SendWithKey (useLocalRadio: {useLocalRadio})");

            var key = new ChannelKey(7);
            using (var keyedLink = useLocalRadio ?
                provider.GetRequiredService<LocalRadio>().OpenWithKey<ITestService, ChannelKey>(new TestService(), key) :
                Radio.OpenWithKey<ITestService, ChannelKey>(new TestService(), key))
            {
                Check(sender.SendWithKey(key).Double(8).TryGetFirst(out var keyed) && keyed == 16, $"ISender with struct key (useLocalRadio: {useLocalRadio})");
            }

            Check(sender.SendWithKey(key).Double(8).IsEmpty, $"Disposed keyed link (useLocalRadio: {useLocalRadio})");
            Check(provider.GetRequiredService<IChannel<IManualService>>() is not null &&
                provider.GetService<IManualService>() is null &&
                provider.GetService<ISender<IManualService>>() is null, $"DI registration opt-out (useLocalRadio: {useLocalRadio})");

            var broker = provider.GetRequiredService<ITestService>();
            Check(broker.Double(6).TryGetFirst(out var b) && b == 12, $"Broker (useLocalRadio: {useLocalRadio})");
        }
    }

    private static void FieldCopierRoundTrip()
    {
        Console.WriteLine("FieldCopier");
        var from = new CopyTarget(Guid.NewGuid()) { Value = 7 };
        from.Prepare();
        var to = new CopyTarget();
        FieldCopier.Copy(ref from, ref to);
        Check(from.Compare(to), "Copy including inherited, init-only and private readonly fields");

        to = new CopyTarget();
        FieldCopier.GetDelegate<CopyTarget>()(ref from, ref to);
        Check(from.Compare(to), "GetDelegate including inherited private fields");
    }
}
