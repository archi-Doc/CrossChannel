// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using CrossChannel;
using Xunit;

namespace XUnitTest;

/// <summary>
/// Keyed channels are stored in a hashtable keyed by (service type, key type).<br/>
/// These tests use enough distinct key types for several of them to land in the same bucket,
/// which is where a lookup that only compares the service type would return the wrong map.
/// </summary>
public class KeyTypeCollisionTest
{
    private struct K01;

    private struct K02;

    private struct K03;

    private struct K04;

    private struct K05;

    private struct K06;

    private struct K07;

    private struct K08;

    private sealed class K09;

    private sealed class K10;

    private sealed class K11;

    private sealed class K12;

    private sealed class K13;

    private sealed class K14;

    private sealed class K15;

    private sealed class K16;

    [Fact]
    public void ManyKeyTypesOnOneService()
    {
        var radio = new RadioClass();

        // Every key type gets its own map, even when several of them share a bucket.
        Open<K01>(radio, 1);
        Open<K02>(radio, 2);
        Open<K03>(radio, 3);
        Open<K04>(radio, 4);
        Open<K05>(radio, 5);
        Open<K06>(radio, 6);
        Open<K07>(radio, 7);
        Open<K08>(radio, 8);
        Open<K09>(radio, 9);
        Open<K10>(radio, 10);
        Open<K11>(radio, 11);
        Open<K12>(radio, 12);
        Open<K13>(radio, 13);
        Open<K14>(radio, 14);
        Open<K15>(radio, 15);
        Open<K16>(radio, 16);
    }

    [Fact]
    public void SameServiceDifferentKeyTypes()
    {
        var radio = new RadioClass();

        using var intLink = radio.OpenWithKey((ITestService)new TestService(), 1);
        using var stringLink = radio.OpenWithKey((ITestService)new TestService(), "1");
        using var longLink = radio.OpenWithKey((ITestService)new TestService(), 1L);

        // The three keys are equal-looking but of different types, so each one addresses its own channel.
        radio.SendWithKey<ITestService, int>(1).Double(1).SequenceEqual([2,]).IsTrue();
        radio.SendWithKey<ITestService, string>("1").Double(2).SequenceEqual([4,]).IsTrue();
        radio.SendWithKey<ITestService, long>(1L).Double(3).SequenceEqual([6,]).IsTrue();

        radio.TryGetChannelWithKey<ITestService, int>(1, out var a).IsTrue();
        radio.TryGetChannelWithKey<ITestService, long>(1L, out var b).IsTrue();
        ReferenceEquals(a, b).IsFalse();
    }

    private static void Open<TKey>(RadioClass radio, int value)
        where TKey : notnull, new()
    {
        var key = new TKey();
        using var link = radio.OpenWithKey((ITestService)new TestService(), key);
        link.IsNotNull();

        radio.TryGetChannelWithKey<ITestService, TKey>(key, out var channel).IsTrue();
        channel!.Count.Is(1);
        radio.SendWithKey<ITestService, TKey>(key).Double(value).SequenceEqual([value * 2,]).IsTrue();
    }
}
