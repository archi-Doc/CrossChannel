// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using CrossChannel;

namespace Benchmark
{
    [Config(typeof(BenchmarkConfig))]
    public class CrossChannelBenchmark2
    {
        public RadioClass TestRadio { get; } = new();

        public List<XChannel> ChannelList = new();

        public CrossChannelBenchmark2()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            ChannelList.Add(Radio.Open<int>(x => { }));
            ChannelList.Add(Radio.OpenKey<int, int>(0, x => { }));
            ChannelList.Add(Radio.OpenTwoWay<int, int>(x => x));
            ChannelList.Add(Radio.OpenTwoWayKey<int, int, int>(0, x => x));

            ChannelList.Add(this.TestRadio.Open<int>(x => { }));
            ChannelList.Add(this.TestRadio.OpenKey<int, int>(0, x => { }));
            ChannelList.Add(this.TestRadio.OpenTwoWay<int, int>(x => x));
            ChannelList.Add(this.TestRadio.OpenTwoWayKey<int, int, int>(0, x => x));

            ChannelList.Add(Radio.Open<short>(x => { }, this));
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
            using (var c = Radio.Open<uint>(x => { }))
            {
            }
        }

        [Benchmark]
        public void Radio_OpenKey()
        {
            using (var c = Radio.OpenKey<uint, uint>(0, x => { }))
            {
            }
        }

        [Benchmark]
        public void Radio_OpenTwoWay()
        {
            using (var c = Radio.OpenTwoWay<uint, uint>(x => x))
            {
            }
        }

        [Benchmark]
        public void Radio_OpenTwoWayKey()
        {
            using (var c = Radio.OpenTwoWayKey<uint, uint, uint>(0, x => x))
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
            return Radio.Send<int>(0);
        }

        [Benchmark]
        public int Radio_SendKey()
        {
            return Radio.SendKey<int, int>(0, 0);
        }

        [Benchmark]
        public int[] Radio_SendTwoWay()
        {
            return Radio.SendTwoWay<int, int>(0);
        }

        [Benchmark]
        public int[] Radio_SendTwoWayKey()
        {
            return Radio.SendTwoWayKey<int, int, int>(0, 0);
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
            using (var c = Radio.Open<ushort>(x => { }, this))
            {
            }
        }

        [Benchmark]
        public int Radio_Weak_Send()
        {
            return Radio.Send<short>(0);
        }
    }
}
