// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using CrossChannel;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class CrossChannelBenchmark
{
    public ObsoleteRadioClass TestRadio { get; } = new();

    public CrossChannelBenchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public void Send()
    {
        ObsoleteRadio.Send<int>(3);
        return;
    }

    [Benchmark]
    public void OpenSend()
    {
        using (var c = ObsoleteRadio.Open<uint>(x => { }))
        {
            ObsoleteRadio.Send<uint>(3);
        }

        return;
    }

    [Benchmark]
    public void OpenSend8()
    {
        using (var c = ObsoleteRadio.Open<uint>(x => { }))
        {
            ObsoleteRadio.Send<uint>(1);
            ObsoleteRadio.Send<uint>(2);
            ObsoleteRadio.Send<uint>(3);
            ObsoleteRadio.Send<uint>(4);
            ObsoleteRadio.Send<uint>(5);
            ObsoleteRadio.Send<uint>(6);
            ObsoleteRadio.Send<uint>(7);
            ObsoleteRadio.Send<uint>(8);
        }

        return;
    }

    public void WeakActionTest(uint x)
    {
    }

    // [Benchmark]
    // public WeakAction<int> CreateWeakAction() => new WeakAction<int>(this, x => { });

    [Benchmark]
    public void OpenSend_Weak()
    {
        using (var c = ObsoleteRadio.Open<uint>(WeakActionTest, new object()))
        {
            ObsoleteRadio.Send<uint>(3);
        }

        return;
    }

    [Benchmark]
    public void OpenSend8_Weak()
    {
        using (var c = ObsoleteRadio.Open<uint>(WeakActionTest, new object()))
        {
            ObsoleteRadio.Send<uint>(1);
            ObsoleteRadio.Send<uint>(2);
            ObsoleteRadio.Send<uint>(3);
            ObsoleteRadio.Send<uint>(4);
            ObsoleteRadio.Send<uint>(5);
            ObsoleteRadio.Send<uint>(6);
            ObsoleteRadio.Send<uint>(7);
            ObsoleteRadio.Send<uint>(8);
        }

        return;
    }

    [Benchmark]
    public void SendKey()
    {
        ObsoleteRadio.SendKey<int, int>(3, 3);
        return;
    }

    [Benchmark]
    public void OpenSend_Key()
    {
        using (var d = ObsoleteRadio.OpenKey<int, uint>(3, x => { }))
        {
            ObsoleteRadio.SendKey<int, uint>(3, 3);
        }

        return;
    }

    [Benchmark]
    public void OpenSend8_Key()
    {
        using (var c = ObsoleteRadio.OpenKey<int, uint>(3, x => { }))
        {
            ObsoleteRadio.SendKey<int, uint>(3, 1);
            ObsoleteRadio.SendKey<int, uint>(3, 2);
            ObsoleteRadio.SendKey<int, uint>(3, 3);
            ObsoleteRadio.SendKey<int, uint>(3, 4);
            ObsoleteRadio.SendKey<int, uint>(3, 5);
            ObsoleteRadio.SendKey<int, uint>(3, 6);
            ObsoleteRadio.SendKey<int, uint>(3, 7);
            ObsoleteRadio.SendKey<int, uint>(3, 8);
        }

        return;
    }

    [Benchmark]
    public void OpenSend_TwoWay()
    {
        using (var c = ObsoleteRadio.OpenTwoWay<int, int>(x => x))
        {
            ObsoleteRadio.SendTwoWay<int, int>(1);
        }

        return;
    }

    [Benchmark]
    public void OpenSend8_TwoWay()
    {
        using (var c = ObsoleteRadio.OpenTwoWay<int, int>(x => x))
        {
            ObsoleteRadio.SendTwoWay<int, int>(1);
            ObsoleteRadio.SendTwoWay<int, int>(2);
            ObsoleteRadio.SendTwoWay<int, int>(3);
            ObsoleteRadio.SendTwoWay<int, int>(4);
            ObsoleteRadio.SendTwoWay<int, int>(5);
            ObsoleteRadio.SendTwoWay<int, int>(6);
            ObsoleteRadio.SendTwoWay<int, int>(7);
            ObsoleteRadio.SendTwoWay<int, int>(8);
        }

        return;
    }

    [Benchmark]
    public void OpenSend_TwoWayKey()
    {
        using (var d = ObsoleteRadio.OpenTwoWayKey<int, uint, uint>(3, x => x))
        {
            ObsoleteRadio.SendTwoWayKey<int, uint, uint>(3, 3);
        }

        return;
    }

    [Benchmark]
    public void OpenSend8_TwoWayKey()
    {
        using (var c = ObsoleteRadio.OpenTwoWayKey<int, uint, uint>(3, x => x))
        {
            ObsoleteRadio.SendTwoWayKey<int, uint, uint>(3, 1);
            ObsoleteRadio.SendTwoWayKey<int, uint, uint>(3, 2);
            ObsoleteRadio.SendTwoWayKey<int, uint, uint>(3, 3);
            ObsoleteRadio.SendTwoWayKey<int, uint, uint>(3, 4);
            ObsoleteRadio.SendTwoWayKey<int, uint, uint>(3, 5);
            ObsoleteRadio.SendTwoWayKey<int, uint, uint>(3, 6);
            ObsoleteRadio.SendTwoWayKey<int, uint, uint>(3, 7);
            ObsoleteRadio.SendTwoWayKey<int, uint, uint>(3, 8);
        }

        return;
    }

    [Benchmark]
    public void Class_Send()
    {
        this.TestRadio.Send<int>(3);
        return;
    }

    [Benchmark]
    public void Class_OpenSend()
    {
        using (var c = this.TestRadio.Open<uint>(x => { }))
        {
            this.TestRadio.Send<uint>(3);
        }

        return;
    }

    [Benchmark]
    public void Class_OpenSend8()
    {
        using (var c = this.TestRadio.Open<uint>(x => { }))
        {
            this.TestRadio.Send<uint>(1);
            this.TestRadio.Send<uint>(2);
            this.TestRadio.Send<uint>(3);
            this.TestRadio.Send<uint>(4);
            this.TestRadio.Send<uint>(5);
            this.TestRadio.Send<uint>(6);
            this.TestRadio.Send<uint>(7);
            this.TestRadio.Send<uint>(8);
        }

        return;
    }

    [Benchmark]
    public void Class_OpenSend_Key()
    {
        using (var c = this.TestRadio.OpenKey<int, uint>(1, x => { }))
        {
            this.TestRadio.SendKey<int, uint>(1, 3);
        }

        return;
    }

    [Benchmark]
    public void Class_OpenSend8_Key()
    {
        using (var c = this.TestRadio.OpenKey<int, uint>(1, x => { }))
        {
            this.TestRadio.SendKey<int, uint>(1, 1);
            this.TestRadio.SendKey<int, uint>(1, 2);
            this.TestRadio.SendKey<int, uint>(1, 3);
            this.TestRadio.SendKey<int, uint>(1, 4);
            this.TestRadio.SendKey<int, uint>(1, 5);
            this.TestRadio.SendKey<int, uint>(1, 6);
            this.TestRadio.SendKey<int, uint>(1, 7);
            this.TestRadio.SendKey<int, uint>(1, 8);
        }

        return;
    }

    [Benchmark]
    public void Class_OpenSend_TwoWay()
    {
        using (var c = this.TestRadio.OpenTwoWay<int, int>(x => x))
        {
            this.TestRadio.SendTwoWay<int, int>(3);
        }

        return;
    }

    [Benchmark]
    public void Class_OpenSend8_TwoWay()
    {
        using (var c = this.TestRadio.OpenTwoWay<int, int>(x => x))
        {
            this.TestRadio.SendTwoWay<int, int>(1);
            this.TestRadio.SendTwoWay<int, int>(2);
            this.TestRadio.SendTwoWay<int, int>(3);
            this.TestRadio.SendTwoWay<int, int>(4);
            this.TestRadio.SendTwoWay<int, int>(5);
            this.TestRadio.SendTwoWay<int, int>(6);
            this.TestRadio.SendTwoWay<int, int>(7);
            this.TestRadio.SendTwoWay<int, int>(8);
        }

        return;
    }

    [Benchmark]
    public void Class_OpenSend_TwoWayKey()
    {
        using (var c = this.TestRadio.OpenTwoWayKey<int, int, int>(1, x => x))
        {
            this.TestRadio.SendTwoWayKey<int, int, int>(1, 3);
        }

        return;
    }

    [Benchmark]
    public void Class_OpenSend8_TwoWayKey()
    {
        using (var c = this.TestRadio.OpenTwoWayKey<int, int, int>(1, x => x))
        {
            this.TestRadio.SendTwoWayKey<int, int, int>(1, 1);
            this.TestRadio.SendTwoWayKey<int, int, int>(1, 2);
            this.TestRadio.SendTwoWayKey<int, int, int>(1, 3);
            this.TestRadio.SendTwoWayKey<int, int, int>(1, 4);
            this.TestRadio.SendTwoWayKey<int, int, int>(1, 5);
            this.TestRadio.SendTwoWayKey<int, int, int>(1, 6);
            this.TestRadio.SendTwoWayKey<int, int, int>(1, 7);
            this.TestRadio.SendTwoWayKey<int, int, int>(1, 8);
        }

        return;
    }
}
