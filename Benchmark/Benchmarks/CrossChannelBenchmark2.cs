// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using CrossChannel;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class CrossChannelBenchmark2
{
    public ObsoleteRadioClass TestRadio { get; } = new();

    public List<XChannel> ChannelList = new();

    public CrossChannelBenchmark2()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        ChannelList.Add(ObsoleteRadio.Open<int>(x => { }));
        ChannelList.Add(ObsoleteRadio.OpenKey<int, int>(0, x => { }));
        ChannelList.Add(ObsoleteRadio.OpenTwoWay<int, int>(x => x));
        ChannelList.Add(ObsoleteRadio.OpenTwoWayKey<int, int, int>(0, x => x));

        ChannelList.Add(this.TestRadio.Open<int>(x => { }));
        ChannelList.Add(this.TestRadio.OpenKey<int, int>(0, x => { }));
        ChannelList.Add(this.TestRadio.OpenTwoWay<int, int>(x => x));
        ChannelList.Add(this.TestRadio.OpenTwoWayKey<int, int, int>(0, x => x));

        ChannelList.Add(ObsoleteRadio.Open<short>(x => { }, this));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var x in this.ChannelList)
        {
            x.Dispose();
        }

        this.ChannelList.Clear();
    }

    [Benchmark]
    public void Radio_Open()
    {
        using (var c = ObsoleteRadio.Open<uint>(x => { }))
        {
        }
    }

    [Benchmark]
    public void Radio_OpenKey()
    {
        using (var c = ObsoleteRadio.OpenKey<uint, uint>(0, x => { }))
        {
        }
    }

    [Benchmark]
    public void Radio_OpenTwoWay()
    {
        using (var c = ObsoleteRadio.OpenTwoWay<uint, uint>(x => x))
        {
        }
    }

    [Benchmark]
    public void Radio_OpenTwoWayKey()
    {
        using (var c = ObsoleteRadio.OpenTwoWayKey<uint, uint, uint>(0, x => x))
        {
        }
    }

    [Benchmark]
    public void Class_Open()
    {
        using (var c = this.TestRadio.Open<uint>(x => { }))
        {
        }
    }

    [Benchmark]
    public void Class_OpenKey()
    {
        using (var c = this.TestRadio.OpenKey<uint, uint>(0, x => { }))
        {
        }
    }

    [Benchmark]
    public void Class_OpenTwoWay()
    {
        using (var c = this.TestRadio.OpenTwoWay<uint, uint>(x => x))
        {
        }
    }

    [Benchmark]
    public void Class_OpenTwoWayKey()
    {
        using (var c = this.TestRadio.OpenTwoWayKey<uint, uint, uint>(0, x => x))
        {
        }
    }

    [Benchmark]
    public int Radio_Send()
    {
        return ObsoleteRadio.Send<int>(0);
    }

    [Benchmark]
    public int Radio_SendKey()
    {
        return ObsoleteRadio.SendKey<int, int>(0, 0);
    }

    [Benchmark]
    public int[] Radio_SendTwoWay()
    {
        return ObsoleteRadio.SendTwoWay<int, int>(0);
    }

    [Benchmark]
    public int[] Radio_SendTwoWayKey()
    {
        return ObsoleteRadio.SendTwoWayKey<int, int, int>(0, 0);
    }

    [Benchmark]
    public int Class_Send()
    {
        return this.TestRadio.Send<int>(0);
    }

    [Benchmark]
    public int Class_SendKey()
    {
        return this.TestRadio.SendKey<int, int>(0, 0);
    }

    [Benchmark]
    public int[] Class_SendTwoWay()
    {
        return this.TestRadio.SendTwoWay<int, int>(0);
    }

    [Benchmark]
    public int[] Class_SendTwoWayKey()
    {
        return this.TestRadio.SendTwoWayKey<int, int, int>(0, 0);
    }

    [Benchmark]
    public void Radio_Weak_Open()
    {
        using (var c = ObsoleteRadio.Open<ushort>(x => { }, this))
        {
        }
    }

    [Benchmark]
    public int Radio_Weak_Send()
    {
        return ObsoleteRadio.Send<short>(0);
    }
}
