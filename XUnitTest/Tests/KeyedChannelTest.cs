// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using CrossChannel;
using Xunit;

namespace XUnitTest;

[RadioService(MaxLinks = 0)]
public interface IZeroLinkService : IRadioService
{
    void Run();
}

public class ZeroLinkService : IZeroLinkService
{
    public int Count { get; private set; }

    void IZeroLinkService.Run() => this.Count++;
}

public class KeyedChannelTest
{
    [Fact]
    public void TryGetChannelWithKey()
    {
        var radio = new LocalRadio();
        radio.TryGetChannelWithKey<ITestService, int>(1, out _).IsFalse();
        radio.TryGetChannelWithKey(typeof(ITestService), 1, out _).IsFalse();

        using (radio.OpenWithKey((ITestService)new TestService(), 1))
        {
            radio.TryGetChannelWithKey<ITestService, int>(1, out var channel).IsTrue();
            channel!.Count.Is(1);

            // The Type-based overload must find the same channel.
            radio.TryGetChannelWithKey(typeof(ITestService), 1, out var channel2).IsTrue();
            ReferenceEquals(channel, channel2).IsTrue();

            radio.TryGetChannelWithKey(typeof(ITestService), 2, out _).IsFalse();
            radio.TryGetChannelWithKey(typeof(ITestService), "1", out _).IsFalse(); // Different key type.
        }

        radio.TryGetChannelWithKey<ITestService, int>(1, out _).IsFalse();
        radio.TryGetChannelWithKey(typeof(ITestService), 1, out _).IsFalse();
    }

    [Fact]
    public void ReuseKeyedChannelInstance()
    {
        var radio = new LocalRadio();
        var link = radio.OpenWithKey((ITestService)new TestService(), 1);
        link.IsNotNull();

        radio.TryGetChannelWithKey<ITestService, int>(1, out var channel).IsTrue();
        link!.Dispose(); // The channel is detached from the keyed map here.

        // Operating on the detached channel instance must not throw.
        using var link2 = channel!.Open(new TestService());
        link2.IsNotNull();
        channel.Count.Is(1);
        channel.GetBroker().Double(3).SequenceEqual([6,]).IsTrue();
    }

    [Fact]
    public void SendWithKey()
    {
        var radio = new LocalRadio();
        radio.SendWithKey<ITestService, int>(1).Double(1).IsEmpty.IsTrue(); // Empty channel.

        using (radio.OpenWithKey((ITestService)new TestService(), 1))
        using (radio.OpenWithKey((ITestService)new TestService(), 2))
        {
            radio.SendWithKey<ITestService, int>(1).Double(1).SequenceEqual([2,]).IsTrue();
            radio.SendWithKey<ITestService, int>(2).Double(2).SequenceEqual([4,]).IsTrue();
            radio.SendWithKey<ITestService, int>(3).Double(3).IsEmpty.IsTrue();
            radio.SendWithKey<ITestService, string>("1").Double(1).IsEmpty.IsTrue();
        }
    }

    [Fact]
    public void NonpositiveMaxLinksRejectsKeyedOpenWithoutLeavingAChannel()
    {
        var radio = new LocalRadio();
        var service = new ZeroLinkService();
        RadioServiceRegistry.GetRegistration<IZeroLinkService>().MaxLinks.Is(0);

        radio.OpenWithKey<IZeroLinkService, int>(service, 1).IsNull();
        radio.TryGetChannelWithKey<IZeroLinkService, int>(1, out _).IsFalse();
        radio.TryGetChannelWithKey(typeof(IZeroLinkService), 1, out _).IsFalse();
        ReferenceEquals(radio.SendWithKey<IZeroLinkService, int>(1), RadioServiceRegistry.GetEmptyChannel<IZeroLinkService>().GetBroker()).IsTrue();
        radio.SendWithKey<IZeroLinkService, int>(1).Run();
        service.Count.Is(0);

        radio.Open<IZeroLinkService>(service).IsNull();
        radio.GetChannel<IZeroLinkService>().MaxLinks.Is(0);
        radio.GetChannel<IZeroLinkService>().Count.Is(0);
    }

    [Fact]
    public void KeyedChannelIsConfiguredLikeAStandaloneChannel()
    {// Both kinds of channel take MaxLinks and a broker of their own from the registration.
        var radio = new LocalRadio();
        using var link = radio.OpenWithKey<ISingleLinkCounterService, int>(new SingleLinkCounterService(), 1);
        link.IsNotNull();
        radio.TryGetChannelWithKey<ISingleLinkCounterService, int>(1, out var keyed).IsTrue();
        var standalone = new Channel<ISingleLinkCounterService>();

        keyed!.MaxLinks.Is(1);
        standalone.MaxLinks.Is(1);
        ReferenceEquals(keyed.GetBroker(), radio.SendWithKey<ISingleLinkCounterService, int>(1)).IsTrue();
        ReferenceEquals(keyed.GetBroker(), standalone.GetBroker()).IsFalse();

        // The keyed channel is full; the rejected link must not detach it.
        radio.OpenWithKey<ISingleLinkCounterService, int>(new SingleLinkCounterService(), 1).IsNull();
        keyed.Count.Is(1);
        radio.TryGetChannelWithKey<ISingleLinkCounterService, int>(1, out var same).IsTrue();
        ReferenceEquals(keyed, same).IsTrue();

        // A standalone channel belongs to no map, and it can be reused after its last link is closed.
        var receiver = new SingleLinkCounterService();
        for (var i = 1; i <= 3; i++)
        {
            using (var standaloneLink = standalone.Open(receiver))
            {
                standaloneLink.IsNotNull();
                standalone.Open(new SingleLinkCounterService()).IsNull(); // Full.
                standalone.GetBroker().Increment();
                radio.SendWithKey<ISingleLinkCounterService, int>(1).Increment();
                receiver.Count.Is(i);
            }

            standalone.Count.Is(0);
        }
    }

    [Fact]
    public void CachedKeyedBrokerStaysBoundToTheDetachedChannel()
    {
        var radio = new LocalRadio();
        var link = radio.OpenWithKey<ITestService, int>(new TestService(), 1)!;
        var cached = radio.SendWithKey<ITestService, int>(1);
        cached.Double(1).SequenceEqual([2,]).IsTrue();
        link.Dispose(); // Detaches the channel from the map.

        using (radio.OpenWithKey<ITestService, int>(new TestService(), 1))
        {
            cached.Double(1).IsEmpty.IsTrue(); // A new channel is attached to the key, but the cached broker still sends to the old one.
            radio.SendWithKey<ITestService, int>(1).Double(1).Count.Is(1);
            ReferenceEquals(cached, radio.SendWithKey<ITestService, int>(1)).IsFalse();
        }
    }

    [Fact]
    public void DirectOpenOnAnAttachedKeyedChannelKeepsItInTheMap()
    {
        var radio = new LocalRadio();
        var keyedLink = radio.OpenWithKey<ITestService, int>(new TestService(), 5)!;
        radio.TryGetChannelWithKey<ITestService, int>(5, out var channel).IsTrue();
        var directLink = channel!.Open(new TestService())!;
        keyedLink.Dispose();

        // The channel still has a link, so it stays in the map.
        radio.TryGetChannelWithKey<ITestService, int>(5, out var channel2).IsTrue();
        ReferenceEquals(channel, channel2).IsTrue();
        radio.SendWithKey<ITestService, int>(5).Double(1).SequenceEqual([2,]).IsTrue();

        directLink.Dispose();
        radio.TryGetChannelWithKey<ITestService, int>(5, out _).IsFalse();
    }

    [Fact]
    public void KeyedChannelsAreIsolatedPerRadio()
    {
        var radio1 = new LocalRadio();
        var radio2 = new LocalRadio();

        using (radio1.OpenWithKey<ITestService, int>(new TestService(), 1))
        {
            radio1.SendWithKey<ITestService, int>(1).Double(1).SequenceEqual([2,]).IsTrue();
            radio2.SendWithKey<ITestService, int>(1).Double(1).IsEmpty.IsTrue();
            radio2.TryGetChannelWithKey<ITestService, int>(1, out _).IsFalse();
            radio2.TryGetChannelWithKey(typeof(ITestService), 1, out _).IsFalse();

            using (radio2.OpenWithKey<ITestService, int>(new TestService(), 1))
            {
                radio1.TryGetChannelWithKey<ITestService, int>(1, out var channel1).IsTrue();
                radio2.TryGetChannelWithKey<ITestService, int>(1, out var channel2).IsTrue();
                ReferenceEquals(channel1, channel2).IsFalse();
                radio1.SendWithKey<ITestService, int>(1).Double(1).Count.Is(1);
            }
        }

        // The static Radio is only read here.
        ReferenceEquals(Radio.GetChannel<ITestService>(), radio1.GetChannel<ITestService>()).IsFalse();
    }

    [Fact]
    public void DeadWeakKeyedLinkDetachesTheChannelOnSend()
    {
        var radio = new LocalRadio();
        var link = OpenTemporary(radio, 9);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(link.TryGetInstance(out _));
        link.IsOpen.IsTrue(); // Not cleaned up until it is found.

        // Sending finds the dead link and closes it, which detaches the emptied channel from the map.
        radio.SendWithKey<ITestService, int>(9).Double(1).IsEmpty.IsTrue();
        link.IsOpen.IsFalse();
        radio.TryGetChannelWithKey<ITestService, int>(9, out _).IsFalse();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Channel<ITestService>.Link OpenTemporary(LocalRadio radio, int key)
        => radio.OpenWithKey<ITestService, int>(new TestService(), key, true)!;
}
