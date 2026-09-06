// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CrossChannel;
using Xunit;
using ServiceAlias = CrossChannel.RadioServiceAttribute;

#pragma warning disable SA1300 // Lowercase keyword identifiers exercise generated name escaping.

namespace XUnitTest;

public interface IHiddenBase : IRadioService
{
    RadioResult<int> Value();
}

[ServiceAlias]
public interface IRegressionService : IHiddenBase
{
    new RadioResult<int> Value();

    RadioResult<int[]> Arrays();

    Task<RadioResult<int[]>> ArraysAsync();

    void @event();

    Task Completed();
}

public class RegressionService : IRegressionService
{
    public Action? Callback { get; set; }

    public int Calls { get; private set; }

    RadioResult<int> IHiddenBase.Value() => new(1);

    RadioResult<int> IRegressionService.Value()
    {
        this.Callback?.Invoke();
        this.Calls++;
        return new(2);
    }

    public RadioResult<int[]> Arrays() => RadioResult<int[]>.Single([3, 4]);

    public Task<RadioResult<int[]>> ArraysAsync() => Task.FromResult(this.Arrays());

    public void @event()
    {
        this.Callback?.Invoke();
        this.Calls++;
    }

    public Task Completed()
    {
        this.Callback?.Invoke();
        this.Calls++;
        return Task.CompletedTask;
    }
}

public class RegressionTest
{
    [Fact]
    public void NestedStructServiceIsRegistered() => StructHost.VerifyRegistration();

    public interface ILateService : IRadioService
    {
    }

    private sealed class LateService : ILateService
    {
    }

    [Fact]
    public void MissingRegistrationCanRecover()
    {
        Assert.Throws<InvalidOperationException>(() => ChannelRegistry.GetRegistration<ILateService>());
        Assert.Throws<InvalidOperationException>(() => Radio.GetChannel<ILateService>());
        Assert.Throws<InvalidOperationException>(() => Radio.Send<ILateService>());
        Assert.True(ChannelRegistry.Register(new(typeof(ILateService), _ => new LateService(), () => new Channel<ILateService>(), 1, false)));
        Assert.Same(ChannelRegistry.GetRegistration(typeof(ILateService)), ChannelRegistry.GetRegistration<ILateService>());
        Assert.Same(Radio.GetChannel(typeof(ILateService)), Radio.GetChannel<ILateService>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FullChannelReclaimsDeadWeakLink(bool keyed)
    {
        var radio = new RadioClass();
        using var dead = OpenTemporary(radio, keyed);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(dead.TryGetInstance(out _));

        var receiver = new SingleLinkCounterService();
        using var link = keyed ? radio.OpenWithKey<ISingleLinkCounterService, int>(receiver, 7) : radio.Open<ISingleLinkCounterService>(receiver);
        Assert.NotNull(link);
        Assert.False(dead.IsValid);
        var broker = keyed ? radio.SendWithKey<ISingleLinkCounterService, int>(7) : radio.Send<ISingleLinkCounterService>();
        broker.Increment();
        Assert.Equal(1, receiver.Count);
    }

    [Fact]
    public void NullSubscriptionDoesNotCreateKeyedChannel()
    {
        var radio = new RadioClass();
        Assert.Throws<ArgumentNullException>(() => radio.Open<IRegressionService>(null!));
        Assert.Equal(0, radio.GetChannel<IRegressionService>().Count);
        Assert.Throws<ArgumentNullException>(() => radio.OpenWithKey<IRegressionService, int>(null!, 3));
        Assert.False(radio.TryGetChannelWithKey<IRegressionService, int>(3, out _));
        Assert.Throws<ArgumentNullException>(() => RadioResult<int>.FromArray(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void NonGenericEnumeratorValidatesStateAndResets(int count)
    {
        IEnumerator enumerator = ((IEnumerable)RadioResult<int>.FromArray(Enumerable.Range(1, count).ToArray())).GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        for (var i = 1; i <= count; i++)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(i, enumerator.Current);
        }

        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Equal(count > 0, enumerator.MoveNext());
    }

    [Fact]
    public async Task AliasedAttributeHiddenMethodsAndArrayResults()
    {
        var channel = new Channel<IRegressionService>();
        using var first = channel.Open(new RegressionService());
        using var second = channel.Open(new RegressionService());
        var broker = channel.GetBroker();
        Assert.Equal(new[] { 2, 2 }, broker.Value().ToArray());
        Assert.Equal(new[] { 1, 1 }, ((IHiddenBase)broker).Value().ToArray());
        Assert.All(broker.Arrays(), x => Assert.Equal(new[] { 3, 4 }, x));
        Assert.Equal(2, broker.Arrays().Count);
        Assert.Equal(2, (await broker.ArraysAsync()).Count);
        Assert.All(await broker.ArraysAsync(), x => Assert.Equal(new[] { 3, 4 }, x));
        broker.@event();
    }

    [Fact]
    public void GrowthDuringDeliveryDoesNotTruncateResults()
    {
        var channel = new Channel<IRegressionService>();
        var receiver = new RegressionService();
        using var first = channel.Open(receiver);
        using var reserve = channel.Open(new RegressionService());
        reserve!.Dispose();
        Channel<IRegressionService>.Link? added = null;
        receiver.Callback = () => added ??= channel.Open(new RegressionService());
        try
        {
            Assert.Equal(2, channel.GetBroker().Value().Count);
        }
        finally
        {
            added?.Dispose();
        }
    }

    [Fact]
    public void DisposedLinkInResizedArrayIsSkipped()
    {
        var channel = new Channel<IRegressionService>();
        var first = new RegressionService();
        using var firstLink = channel.Open(first);
        var removed = new RegressionService();
        using var removedLink = channel.Open(removed);
        using var third = channel.Open(new RegressionService());
        using var fourth = channel.Open(new RegressionService());
        Channel<IRegressionService>.Link? added = null;
        first.Callback = () =>
        {
            added = channel.Open(new RegressionService()); // Publish a new array during delivery.
            removedLink!.Dispose();
        };
        try
        {
            channel.GetBroker().@event();
            Assert.Equal(0, removed.Calls);
        }
        finally
        {
            added?.Dispose();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task FillingAHoleDoesNotSkipAStableReceiver(int mode)
    {
        var channel = new Channel<IRegressionService>();
        var first = new RegressionService();
        var stable = new RegressionService();
        using var firstLink = channel.Open(first);
        using var hole1 = channel.Open(new RegressionService());
        using var hole2 = channel.Open(new RegressionService());
        using var stableLink = channel.Open(stable);
        hole1!.Dispose();
        hole2!.Dispose();
        Channel<IRegressionService>.Link? added = null;
        first.Callback = () => added ??= channel.Open(new RegressionService());
        try
        {
            var broker = channel.GetBroker();
            if (mode == 0)
            {
                broker.@event();
            }
            else if (mode == 1)
            {
                broker.Value();
            }
            else
            {
                await broker.Completed();
            }

            Assert.Equal(1, stable.Calls);
        }
        finally
        {
            added?.Dispose();
        }
    }

    [Fact]
    public async Task TypeCachesSurviveConcurrentGrowth()
    {
        var radio = new RadioClass();
        var registrations = ChannelRegistry.Registrations.ToArray();
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(
            () =>
            {
                foreach (var registration in registrations)
                {
                    var type = registration.ServiceType;
                    Assert.Same(radio.GetChannel(type), radio.GetChannel(type));
                    Assert.Same(Radio.GetChannel(type), Radio.GetChannel(type));
                }
            },
            TestContext.Current.CancellationToken)));
    }

    [Fact]
    public async Task AggregateCancellationIsPreserved()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var task = RadioTask.Aggregate(Task.FromCanceled<RadioResult<int>[]>(cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.True(task.IsCanceled);
    }

    [Fact]
    public void SparseChannelShrinksAndPreservesLinkIndices()
    {
        var channel = new Channel<IRegressionService>();
        var links = Enumerable.Range(0, 64).Select(_ => channel.Open(new RegressionService())!).ToArray();
        var oldLength = channel.UnsafeGetLinks().Links.Length;
        foreach (var link in links.Take(62))
        {
            link.Dispose();
        }

        for (var i = 0; i < Channel.TrimThreshold; i++)
        {
            using var temporary = channel.Open(new RegressionService());
        }

        Assert.True(channel.UnsafeGetLinks().Links.Length < oldLength);
        Assert.Equal(2, channel.GetBroker().Value().Count);
        links[62].Dispose();
        links[63].Dispose();
        Assert.Equal(0, channel.Count);
        using var reused = channel.Open(new RegressionService());
        Assert.Equal(1, channel.GetBroker().Value().Count);
    }

    [Fact]
    public void PeriodicCleanupReclaimsWeakLinksWithoutSending()
    {
        var channel = new Channel<IRegressionService>();
        using var dead = OpenTemporaryRegression(channel);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(dead.TryGetInstance(out _));
        var receiver = new RegressionService();
        for (var i = 0; i < Channel.TrimThreshold * Channel.WeakReferenceCheckThreshold; i++)
        {
            using var temporary = channel.Open(receiver);
        }

        Assert.False(dead.IsValid);
        Assert.Equal(0, channel.Count);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Channel<IRegressionService>.Link OpenTemporaryRegression(Channel<IRegressionService> channel)
        => channel.Open(new RegressionService(), true)!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Channel<ISingleLinkCounterService>.Link OpenTemporary(RadioClass radio, bool keyed)
        => (keyed ? radio.OpenWithKey<ISingleLinkCounterService, int>(new SingleLinkCounterService(), 7, true) : radio.Open<ISingleLinkCounterService>(new SingleLinkCounterService(), true))!;
}

public partial struct StructHost
{
    [RadioService]
    public interface IService : IRadioService
    {
        RadioResult<int> Get();
    }

    private sealed class Service : IService
    {
        public RadioResult<int> Get() => new(7);
    }

    public static void VerifyRegistration()
    {
        var channel = new Channel<IService>();
        using var link = channel.Open(new Service());
        Assert.Equal(new[] { 7 }, channel.GetBroker().Get().ToArray());
    }
}
