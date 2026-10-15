// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossChannel;
using Xunit;

namespace XUnitTest;

/// <summary>
/// Keyed channels of the static <see cref="Radio"/>, whose keyed map is cached per service type and key type once it exists.<br/>
/// Each test opens channels only with its own private key types, so that it starts before the map of its key type exists.
/// </summary>
[Collection(StaticRadioCollection.Name)]
public class StaticKeyedTest
{
    private readonly record struct LifetimeKey(int Value);

    private readonly record struct IsolationKey(int Value);

    private readonly record struct OtherIsolationKey(int Value);

    private sealed record ReferenceKey(string Value);

    private sealed record NullTestKey(string Value);

    private readonly record struct RaceKey(int Value);

    [Fact]
    public void ChannelIsFoundBeforeAndAfterTheMapIsCached()
    {
        var emptyBroker = RadioServiceRegistry.GetEmptyChannel<ITestService>().GetBroker();

        // No channel has been opened with this key type yet, so the map does not exist.
        Radio.TryGetChannelWithKey<ITestService, LifetimeKey>(new(1), out _).IsFalse();
        Radio.TryGetChannelWithKey(typeof(ITestService), new LifetimeKey(1), out _).IsFalse();
        ReferenceEquals(Radio.SendWithKey<ITestService, LifetimeKey>(new(1)), emptyBroker).IsTrue();

        Channel<ITestService> first;
        using (var link = Radio.OpenWithKey<ITestService, LifetimeKey>(new TestService(), new(1)))
        {
            link.IsNotNull();
            Radio.TryGetChannelWithKey<ITestService, LifetimeKey>(new(1), out var found).IsTrue();
            first = found!;
            Radio.TryGetChannelWithKey(typeof(ITestService), new LifetimeKey(1), out var untyped).IsTrue();
            ReferenceEquals(first, untyped).IsTrue();
            first.Count.Is(1);

            Radio.SendWithKey<ITestService, LifetimeKey>(new(1)).Double(2).SequenceEqual([4,]).IsTrue();
            ReferenceEquals(Radio.SendWithKey<ITestService, LifetimeKey>(new(2)), emptyBroker).IsTrue();
            Radio.TryGetChannelWithKey<ITestService, LifetimeKey>(new(2), out _).IsFalse();

            // A second link on the same key joins the same channel.
            using (Radio.OpenWithKey<ITestService, LifetimeKey>(new TestService(), new(1)))
            {
                Radio.TryGetChannelWithKey<ITestService, LifetimeKey>(new(1), out var same).IsTrue();
                ReferenceEquals(first, same).IsTrue();
                first.Count.Is(2);
                Radio.SendWithKey<ITestService, LifetimeKey>(new(1)).Double(1).SequenceEqual([2, 2,]).IsTrue();
            }
        }

        // Closing the last link detaches the channel, although the map stays cached.
        first.Count.Is(0);
        Radio.TryGetChannelWithKey<ITestService, LifetimeKey>(new(1), out _).IsFalse();
        Radio.TryGetChannelWithKey(typeof(ITestService), new LifetimeKey(1), out _).IsFalse();
        ReferenceEquals(Radio.SendWithKey<ITestService, LifetimeKey>(new(1)), emptyBroker).IsTrue();

        // Opening the key again attaches a new channel to the cached map.
        using (Radio.OpenWithKey<ITestService, LifetimeKey>(new TestService(), new(1)))
        using (first.Open(new TestService()))
        {
            Radio.TryGetChannelWithKey<ITestService, LifetimeKey>(new(1), out var second).IsTrue();
            ReferenceEquals(first, second).IsFalse();
            Radio.TryGetChannelWithKey(typeof(ITestService), new LifetimeKey(1), out var untyped).IsTrue();
            ReferenceEquals(second, untyped).IsTrue();

            // The detached channel still works on its own, but it is no longer reached by the key.
            Radio.SendWithKey<ITestService, LifetimeKey>(new(1)).Double(3).SequenceEqual([6,]).IsTrue();
            first.GetBroker().Double(4).SequenceEqual([8,]).IsTrue();
        }

        Radio.TryGetChannelWithKey<ITestService, LifetimeKey>(new(1), out _).IsFalse();
    }

    [Fact]
    public void KeyTypesAndServiceTypesAreIsolated()
    {
        var test = new TestService();
        var counter = new CounterService();
        using var testLink = Radio.OpenWithKey<ITestService, IsolationKey>(test, new(1));
        using var referenceLink = Radio.OpenWithKey<ITestService, ReferenceKey>(test, new("a"));

        // The same key value of another key type, or of another service type, addresses another map.
        Radio.TryGetChannelWithKey<ITestService, OtherIsolationKey>(new(1), out _).IsFalse();
        Radio.SendWithKey<ITestService, OtherIsolationKey>(new(1)).Double(1).IsEmpty.IsTrue();
        Radio.TryGetChannelWithKey<ICounterService, IsolationKey>(new(1), out _).IsFalse();
        Radio.SendWithKey<ICounterService, IsolationKey>(new(1)).Increment();
        counter.Count.Is(0);

        using (Radio.OpenWithKey<ICounterService, IsolationKey>(counter, new(1)))
        using (Radio.OpenWithKey<ITestService, OtherIsolationKey>(new TestService(), new(1)))
        {
            Radio.TryGetChannelWithKey<ITestService, IsolationKey>(new(1), out var testChannel).IsTrue();
            Radio.TryGetChannelWithKey<ITestService, OtherIsolationKey>(new(1), out var otherChannel).IsTrue();
            Radio.TryGetChannelWithKey<ICounterService, IsolationKey>(new(1), out var counterChannel).IsTrue();
            ReferenceEquals(testChannel, otherChannel).IsFalse();
            ReferenceEquals(testChannel, counterChannel).IsFalse();

            Radio.SendWithKey<ICounterService, IsolationKey>(new(1)).Increment();
            counter.Count.Is(1);
            Radio.SendWithKey<ITestService, IsolationKey>(new(1)).Double(1).SequenceEqual([2,]).IsTrue();
            Radio.SendWithKey<ITestService, OtherIsolationKey>(new(1)).Double(2).SequenceEqual([4,]).IsTrue();
        }

        // A reference type key is compared by value (record equality), not by reference.
        Radio.TryGetChannelWithKey<ITestService, ReferenceKey>(new("a"), out var referenceChannel).IsTrue();
        Radio.TryGetChannelWithKey(typeof(ITestService), new ReferenceKey("a"), out var untyped).IsTrue();
        ReferenceEquals(referenceChannel, untyped).IsTrue();
        Radio.SendWithKey<ITestService, ReferenceKey>(new("a")).Double(5).SequenceEqual([10,]).IsTrue();
        Radio.SendWithKey<ITestService, ReferenceKey>(new("b")).Double(5).IsEmpty.IsTrue();
        Radio.SendWithKey<ITestService, string>("a").Double(5).IsEmpty.IsTrue();
    }

    [Fact]
    public void NullArgumentsAreRejected()
    {
        Assert.Throws<ArgumentNullException>("instance", () => Radio.OpenWithKey<ITestService, NullTestKey>(null!, new("a")));
        Assert.Throws<ArgumentNullException>("key", () => Radio.OpenWithKey<ITestService, NullTestKey>(new TestService(), null!));
        Assert.Throws<ArgumentNullException>("serviceType", () => Radio.TryGetChannelWithKey(null!, new NullTestKey("a"), out _));
        Radio.TryGetChannelWithKey<ITestService, NullTestKey>(new("a"), out _).IsFalse();

        // The same once the map exists and is cached.
        using var link = Radio.OpenWithKey<ITestService, NullTestKey>(new TestService(), new("a"));
        Assert.Throws<ArgumentNullException>("instance", () => Radio.OpenWithKey<ITestService, NullTestKey>(null!, new("a")));
        Assert.Throws<ArgumentNullException>("instance", () => Radio.OpenWithKey<ITestService, NullTestKey>(null!, new("b")));
        Assert.Throws<ArgumentNullException>("key", () => Radio.OpenWithKey<ITestService, NullTestKey>(new TestService(), null!));
        Radio.TryGetChannelWithKey<ITestService, NullTestKey>(new("a"), out var channel).IsTrue();
        channel!.Count.Is(1);
        Radio.TryGetChannelWithKey<ITestService, NullTestKey>(new("b"), out _).IsFalse();
    }

    [Fact]
    public async Task ConcurrentFirstOpensShareOneMap()
    {// The first OpenWithKey of a key type creates and caches the map; racing threads must all end up in the same channel.
        const int threads = 8;
        var links = new Channel<ITestService>.Link?[threads];
        using (var barrier = new Barrier(threads))
        {
            var tasks = Enumerable.Range(0, threads)
                .Select(i => Task.Factory.StartNew(
                    () =>
                    {
                        barrier.SignalAndWait(TestContext.Current.CancellationToken);
                        links[i] = Radio.OpenWithKey<ITestService, RaceKey>(new TestService(), new(1));
                    },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default))
                .ToArray();
            await Task.WhenAll(tasks);
        }

        try
        {
            links.All(x => x is not null).IsTrue();
            Radio.TryGetChannelWithKey<ITestService, RaceKey>(new(1), out var channel).IsTrue();
            channel!.Count.Is(threads);
            Radio.SendWithKey<ITestService, RaceKey>(new(1)).Double(1).Count.Is(threads);
        }
        finally
        {
            foreach (var x in links)
            {
                x?.Dispose();
            }
        }

        Radio.TryGetChannelWithKey<ITestService, RaceKey>(new(1), out _).IsFalse();
    }
}
