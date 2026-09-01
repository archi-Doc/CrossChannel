// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using Xunit;

namespace XUnitTest;

public class KeyedChannelTest
{
    [Fact]
    public void TryGetChannelWithKey()
    {
        var radio = new RadioClass();
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
        var radio = new RadioClass();
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
        var radio = new RadioClass();
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
}
