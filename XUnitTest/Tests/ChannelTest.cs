// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using Xunit;

namespace XUnitTest;

public class ChannelTest
{
    [Fact]
    public void GetChannelReturnsTheSameInstance()
    {
        var radio = new RadioClass();

        var channel = radio.GetChannel<ITestService>();
        ReferenceEquals(channel, radio.GetChannel<ITestService>()).IsTrue();
        ReferenceEquals(channel, radio.GetChannel(typeof(ITestService))).IsTrue();

        // Each RadioClass instance owns its own channel.
        ReferenceEquals(channel, new RadioClass().GetChannel<ITestService>()).IsFalse();
    }

    [Fact]
    public void GetChannelWithUnregisteredType()
    {
        var radio = new RadioClass();
        Assert.Throws<InvalidOperationException>(() => radio.GetChannel(typeof(IDisposable)));
    }

    [Fact]
    public void BrokerIsStableAndBoundToTheChannel()
    {
        var radio = new RadioClass();
        var channel = radio.GetChannel<ITestService>();

        ReferenceEquals(channel.GetBroker(), radio.Send<ITestService>()).IsTrue();
        ReferenceEquals(radio.Send<ITestService>(), radio.Send<ITestService>()).IsTrue();
    }

    [Fact]
    public void LinkLifetime()
    {
        var radio = new RadioClass();
        var channel = radio.GetChannel<ITestService>();
        channel.Count.Is(0);

        var link = channel.Open(new TestService());
        link.IsNotNull();
        link!.IsValid.IsTrue();
        channel.Count.Is(1);

        link.Dispose();
        link.IsValid.IsFalse();
        channel.Count.Is(0);

        // Dispose/Close must be idempotent.
        link.Dispose();
        link.Close();
        channel.Count.Is(0);
        channel.GetBroker().Double(1).IsEmpty.IsTrue();
    }

    [Fact]
    public void CloseAndReopen()
    {
        var radio = new RadioClass();
        var channel = radio.GetChannel<ITestService>();

        for (var i = 0; i < 5; i++)
        {
            using var link = channel.Open(new TestService());
            channel.Count.Is(1);
            channel.GetBroker().Double(2).SequenceEqual([4,]).IsTrue();
        }

        channel.Count.Is(0);
    }

    [Fact]
    public void OpenAndCloseManyTimes()
    {// The internal list is trimmed while links are opened; the count must stay consistent.
        var radio = new RadioClass();
        var channel = radio.GetChannel<ITestService>();

        for (var round = 0; round < 4; round++)
        {
            var links = Enumerable.Range(0, Channel.TrimThreshold * 2).Select(_ => channel.Open(new TestService())!).ToArray();
            channel.Count.Is(links.Length);
            channel.GetBroker().Double(1).Count.Is(links.Length);

            foreach (var x in links)
            {
                x.Dispose();
            }

            channel.Count.Is(0);
            channel.GetBroker().Double(1).IsEmpty.IsTrue();
        }
    }

    [Fact]
    public void MaxLinks()
    {
        var radio = new RadioClass();
        var channel = radio.GetChannel<ISingleService>();
        channel.MaxLinks.Is(1);

        using var link = channel.Open(new SingleService());
        link.IsNotNull();
        channel.Open(new SingleService()).IsNull(); // Full.
        channel.Count.Is(1);

        // The default MaxLinks is int.MaxValue.
        radio.GetChannel<ITestService>().MaxLinks.Is(int.MaxValue);
    }

    [Fact]
    public void EmptyChannel()
    {
        var channel = ChannelRegistry.GetEmptyChannel<ITestService>();
        channel.MaxLinks.Is(0);
        channel.Open(new TestService()).IsNull();
        channel.Count.Is(0);
        channel.GetBroker().Double(1).IsEmpty.IsTrue();

        // The empty channel is cached.
        ReferenceEquals(channel, ChannelRegistry.GetEmptyChannel<ITestService>()).IsTrue();
    }

    [Fact]
    public void WeakReferenceLink()
    {
        var radio = new RadioClass();
        var channel = radio.GetChannel<ITestService>();

        void OpenTemporaryInstance() => channel.Open(new TestService(), true);

        OpenTemporaryInstance();
        channel.Count.Is(1);
        channel.GetBroker().Double(1).SequenceEqual([2,]).IsTrue();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // The dead link is removed when it is found during the enumeration.
        channel.GetBroker().Double(1).IsEmpty.IsTrue();
        channel.Count.Is(0);
    }

    [Fact]
    public void StrongReferenceLinkKeepsTheInstanceAlive()
    {
        var radio = new RadioClass();
        var channel = radio.GetChannel<ITestService>();

        void OpenTemporaryInstance() => channel.Open(new TestService());

        OpenTemporaryInstance();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        channel.GetBroker().Double(3).SequenceEqual([6,]).IsTrue();
    }

    [Fact]
    public void UnsafeGetLinks()
    {
        var radio = new RadioClass();
        var channel = radio.GetChannel<ITestService>();

        var (array, countHint) = channel.UnsafeGetLinks();
        countHint.Is(0);
        array.Count(x => x is not null).Is(0);

        var links = Enumerable.Range(0, 10).Select(_ => channel.Open(new TestService())!).ToArray();
        (array, countHint) = channel.UnsafeGetLinks();
        countHint.Is(10);

        // CountHint must never exceed the number of links held by the array.
        array.Count(x => x is not null).Is(countHint);

        links[3].Dispose();
        links[7].Dispose();
        (array, countHint) = channel.UnsafeGetLinks();
        countHint.Is(8);
        array.Count(x => x is not null).Is(countHint);

        foreach (var x in links)
        {
            x.Dispose();
        }
    }
}
