// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossChannel;
using Xunit;

namespace XUnitTest;

[RadioService]
public interface ICounterService : IRadioService
{
    void Increment();

    RadioResult<int> One();

    Task IncrementAsync();
}

public class CounterService : ICounterService
{
    private int count;

    public int Count => Volatile.Read(ref this.count);

    void ICounterService.Increment() => Interlocked.Increment(ref this.count);

    RadioResult<int> ICounterService.One() => new(1);

    Task ICounterService.IncrementAsync()
    {
        Interlocked.Increment(ref this.count);
        return Task.CompletedTask;
    }
}

[RadioService(MaxLinks = 1)]
public interface ISingleLinkCounterService : IRadioService
{
    void Increment();
}

public class SingleLinkCounterService : ISingleLinkCounterService
{
    private int count;

    public int Count => Volatile.Read(ref this.count);

    void ISingleLinkCounterService.Increment() => Interlocked.Increment(ref this.count);
}

public class ConcurrencyTest
{
    private const int Threads = 4;
    private const int Iterations = 5_000;

    [Fact]
    public void ConcurrentSendWithStableReceivers()
    {
        var radio = new RadioClass();
        var services = Enumerable.Range(0, 4).Select(_ => new CounterService()).ToArray();
        var links = services.Select(x => radio.Open<ICounterService>(x)!).ToArray();

        Parallel.For(0, Threads, _ =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                radio.Send<ICounterService>().Increment();
            }
        });

        foreach (var x in services)
        {// Every receiver must have received every message.
            x.Count.Is(Threads * Iterations);
        }

        foreach (var x in links)
        {
            x.Dispose();
        }
    }

    [Fact]
    public void ConcurrentResultSend()
    {
        var radio = new RadioClass();
        var links = Enumerable.Range(0, 4).Select(_ => radio.Open<ICounterService>(new CounterService())!).ToArray();

        Parallel.For(0, Threads, _ =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                radio.Send<ICounterService>().One().Count.Is(4);
            }
        });

        foreach (var x in links)
        {
            x.Dispose();
        }
    }

    [Fact]
    public async Task ConcurrentAsyncSend()
    {
        var radio = new RadioClass();
        var services = Enumerable.Range(0, 4).Select(_ => new CounterService()).ToArray();
        var links = services.Select(x => radio.Open<ICounterService>(x)!).ToArray();

        var tasks = Enumerable.Range(0, Threads).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                await radio.Send<ICounterService>().IncrementAsync();
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        foreach (var x in services)
        {
            x.Count.Is(Threads * Iterations);
        }

        foreach (var x in links)
        {
            x.Dispose();
        }
    }

    [Fact]
    public async Task SendWhileOpeningAndClosing()
    {// Opening and closing links while sending must neither throw nor drop a message for the stable receiver.
        var radio = new RadioClass();
        var stable = new CounterService();
        using var stableLink = radio.Open<ICounterService>(stable); // The first link of the channel.

        using var cts = new CancellationTokenSource();
        var churn = Task.Run(OpenAndCloseUntilCancelled, TestContext.Current.CancellationToken);

        void OpenAndCloseUntilCancelled()
        {
            while (!cts.IsCancellationRequested)
            {
                var links = new List<Channel<ICounterService>.Link>();
                for (var i = 0; i < 8; i++)
                {
                    if (radio.Open<ICounterService>(new CounterService()) is { } link)
                    {
                        links.Add(link);
                    }
                }

                foreach (var x in links)
                {
                    x.Dispose();
                }
            }
        }

        const int count = 20_000;
        for (var i = 0; i < count; i++)
        {
            radio.Send<ICounterService>().Increment();
            radio.Send<ICounterService>().One();
        }

        await cts.CancelAsync();
        await churn;

        stable.Count.Is(count);
        radio.GetChannel<ICounterService>().Count.Is(1);
    }

    [Fact]
    public void ConcurrentDisposeOfTheSameLink()
    {
        var radio = new RadioClass();
        var channel = radio.GetChannel<ICounterService>();

        for (var round = 0; round < 200; round++)
        {
            var link = channel.Open(new CounterService())!;
            Parallel.For(0, Threads, _ => link.Dispose());
            link.IsValid.IsFalse();
            channel.Count.Is(0);
        }
    }

    [Fact]
    public void ConcurrentKeyedChannels()
    {// Each thread uses its own key, so the delivery is deterministic.
        var radio = new RadioClass();

        Parallel.For(0, Threads, t =>
        {
            for (var i = 0; i < 500; i++)
            {
                var service = new CounterService();
                using (radio.OpenWithKey<ICounterService, int>(service, t))
                {
                    radio.SendWithKey<ICounterService, int>(t).Increment();
                    service.Count.Is(1);
                }

                // The channel is detached once its last link is closed.
                radio.TryGetChannelWithKey<ICounterService, int>(t, out _).IsFalse();
            }
        });

        for (var t = 0; t < Threads; t++)
        {
            radio.SendWithKey<ICounterService, int>(t).One().IsEmpty.IsTrue();
        }
    }

    [Fact]
    public async Task ConcurrentKeyedChannelsWithASharedKey()
    {// OpenWithKey must be atomic: a link must never be attached to a channel which was just detached from the map.
        const int key = 1;
        var radio = new RadioClass();
        var threadCount = Math.Max(8, Environment.ProcessorCount);

        void OpenAndClose()
        {
            for (var i = 0; i < 5_000; i++)
            {
                var service = new SingleLinkCounterService();

                // MaxLinks is 1, so this link is always the only one of its channel and the delivery
                // is deterministic. Closing it empties the channel, which detaches it from the map:
                // exactly the window in which a non-atomic OpenWithKey would produce an orphan.
                using (var link = radio.OpenWithKey<ISingleLinkCounterService, int>(service, key))
                {
                    if (link is null)
                    {// Another thread currently holds the single link.
                        continue;
                    }

                    radio.SendWithKey<ISingleLinkCounterService, int>(key).Increment();
                    service.Count.Is(1);
                }
            }
        }

        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Factory.StartNew(OpenAndClose, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default))
            .ToArray();
        await Task.WhenAll(tasks);

        // The channel is detached once the last link is closed.
        radio.TryGetChannelWithKey<ISingleLinkCounterService, int>(key, out _).IsFalse();
    }

    [Fact]
    public void ConcurrentGetChannel()
    {
        var radio = new RadioClass();
        var channels = new Channel<ICounterService>[Threads];

        Parallel.For(0, Threads, t => channels[t] = radio.GetChannel<ICounterService>());

        foreach (var x in channels)
        {// The channel must be created exactly once.
            ReferenceEquals(x, channels[0]).IsTrue();
        }
    }
}
