// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CrossChannel;
using Xunit;

namespace XUnitTest;

/// <summary>
/// The link array of a channel (seen through <see cref="Channel{TService}.DangerousGetLinks"/>):
/// it grows by doubling, reuses the slots of closed links, and shrinks when most of it is unused.
/// </summary>
public class LinkListTest
{
    [Fact]
    public void ArrayGrowsByDoublingAndFillsSlotsInOrder()
    {
        var channel = new Channel<ITestService>();
        channel.DangerousGetLinks().Links.Length.Is(1);

        var links = new List<Channel<ITestService>.Link>();
        for (var n = 1; n <= 100; n++)
        {
            links.Add(channel.Open(new TestService())!);
            var (array, countHint) = channel.DangerousGetLinks();
            array.Length.Is((int)BitOperations.RoundUpToPowerOf2((uint)n));
            countHint.Is(n);
            channel.Count.Is(n);
            for (var i = 0; i < array.Length; i++)
            {// A fresh channel fills its slots in order.
                ReferenceEquals(array[i], i < n ? links[i] : null).IsTrue();
            }
        }

        channel.GetBroker().Double(1).Count.Is(100);
        foreach (var x in links)
        {
            x.Dispose();
        }

        channel.Count.Is(0);
    }

    [Fact]
    public void ClosedSlotsAreReusedBeforeTheArrayGrows()
    {
        var channel = new Channel<ITestService>();
        var links = Enumerable.Range(0, 8).Select(_ => channel.Open(new TestService())!).ToArray();
        var array = channel.DangerousGetLinks().Links;
        array.Length.Is(8);

        links[5].Dispose();
        links[2].Dispose();
        links[5].IsOpen.IsFalse();
        channel.Count.Is(6);

        // The freed slots are reused in the order they were freed, and the array is kept.
        var reused1 = channel.Open(new TestService())!;
        var reused2 = channel.Open(new TestService())!;
        ReferenceEquals(channel.DangerousGetLinks().Links, array).IsTrue();
        ReferenceEquals(array[5], reused1).IsTrue();
        ReferenceEquals(array[2], reused2).IsTrue();
        channel.Count.Is(8);

        // The array is full, so the next link grows it and takes the first new slot.
        var grown = channel.Open(new TestService())!;
        var newArray = channel.DangerousGetLinks().Links;
        newArray.Length.Is(16);
        ReferenceEquals(newArray[8], grown).IsTrue();
        for (var i = 0; i < 8; i++)
        {
            ReferenceEquals(newArray[i], array[i]).IsTrue();
        }

        // The remaining new slots are used in order.
        var next = channel.Open(new TestService())!;
        ReferenceEquals(channel.DangerousGetLinks().Links[9], next).IsTrue();
        channel.GetBroker().Double(1).Count.Is(10);
    }

    [Fact]
    public void ArrayShrinksAfterLinksAreClosedAndGrowsAgain()
    {
        var channel = new Channel<ITestService>();
        var survivor = channel.Open(new TestService())!;
        var links = Enumerable.Range(0, 99).Select(_ => channel.Open(new TestService())!).ToArray();
        channel.DangerousGetLinks().Links.Length.Is(128);

        foreach (var x in links)
        {
            x.Dispose();
        }

        // Open and close a link until a trim runs at the low point.
        for (var i = 0; i < Channel.TrimThreshold * 2; i++)
        {
            using var temporary = channel.Open(new TestService());
        }

        var shrunk = channel.DangerousGetLinks().Links;
        shrunk.Length.Is(x => x <= 8);
        AssertConsistent(channel, [survivor,]);
        channel.GetBroker().Double(1).SequenceEqual([2,]).IsTrue();

        // It grows again when needed.
        links = Enumerable.Range(0, 99).Select(_ => channel.Open(new TestService())!).ToArray();
        channel.DangerousGetLinks().Links.Length.Is(128);
        AssertConsistent(channel, [survivor, .. links,]);
        channel.GetBroker().Double(1).Count.Is(100);
    }

    [Fact]
    public void RandomChurnKeepsTheLinkArrayConsistent()
    {
        var random = new Random(12345);
        var channel = new Channel<ITestService>();
        var open = new List<Channel<ITestService>.Link>();
        var closed = new List<Channel<ITestService>.Link>();
        var maxLength = 0;
        var minLengthAfterPeak = int.MaxValue;

        for (var step = 0; step < 20_000; step++)
        {
            // Rise to a few hundred links, then fall back to a few, repeatedly.
            var rising = (step / 1_000) % 2 == 0;
            var openProbability = rising ? 0.7 : (open.Count > 2 ? 0.3 : 0.5);
            if (open.Count == 0 || random.NextDouble() < openProbability)
            {
                var link = channel.Open(new TestService())!;
                link.IsOpen.IsTrue();
                open.Add(link);
            }
            else
            {
                var index = random.Next(open.Count);
                var link = open[index];
                open[index] = open[^1];
                open.RemoveAt(open.Count - 1);
                link.Dispose();
                link.IsOpen.IsFalse();
                closed.Add(link);
            }

            AssertConsistent(channel, open);
            var length = channel.DangerousGetLinks().Links.Length;
            maxLength = Math.Max(maxLength, length);
            if (maxLength >= 256)
            {
                minLengthAfterPeak = Math.Min(minLengthAfterPeak, length);
            }
        }

        // The array both grew and shrank during the churn.
        maxLength.Is(x => x >= 256);
        minLengthAfterPeak.Is(x => x <= 16);

        // Closed links stay closed, and closing them again is a no-op.
        foreach (var x in closed)
        {
            x.Dispose();
            x.IsOpen.IsFalse();
        }

        AssertConsistent(channel, open);
    }

    [Fact]
    public void MaxLinksIsEnforcedUnderChurn()
    {
        var channel = new Channel<ISingleLinkCounterService>();
        channel.MaxLinks.Is(1);

        for (var i = 0; i < Channel.TrimThreshold * 4; i++)
        {
            var receiver = new SingleLinkCounterService();
            var link = channel.Open(receiver);
            link.IsNotNull();
            channel.Open(new SingleLinkCounterService()).IsNull(); // Full.
            channel.Count.Is(1);
            channel.DangerousGetLinks().Links.Length.Is(1); // A rejected link must not grow the array.

            channel.GetBroker().Increment();
            receiver.Count.Is(1);
            link!.Dispose();
            channel.Count.Is(0);
        }
    }

    private static void AssertConsistent<TService>(Channel<TService> channel, IReadOnlyCollection<Channel<TService>.Link> open)
        where TService : class, IRadioService
    {
        var (array, countHint) = channel.DangerousGetLinks();
        channel.Count.Is(open.Count);
        countHint.Is(open.Count);
        BitOperations.IsPow2(array.Length).IsTrue();
        array.Length.Is(x => x >= open.Count);

        // Every open link is stored exactly once, and nothing else is stored.
        var stored = array.OfType<Channel<TService>.Link>().ToArray();
        stored.Length.Is(open.Count);
        new HashSet<Channel<TService>.Link>(stored, ReferenceEqualityComparer.Instance).SetEquals(open).IsTrue();
    }
}
