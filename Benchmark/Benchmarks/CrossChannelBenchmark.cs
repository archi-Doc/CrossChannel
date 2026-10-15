// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using CrossChannel;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class CrossChannelBenchmark
{
    public LocalRadio TestRadio { get; } = new();

    private ISimpleService simpleService1 = new SimpleService();

    [Benchmark]
    public void Send()
    {
        Radio.Send<ISimpleService>().Test(1);
        return;
    }

    [Benchmark]
    public void OpenSend()
    {
        using (Radio.Open(this.simpleService1))
        {
            Radio.Send<ISimpleService>().Test(1);
        }

        return;
    }

    [Benchmark]
    public void OpenSend8()
    {
        using (Radio.Open(this.simpleService1))
        {
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(1);
        }

        return;
    }

    [Benchmark]
    public void OpenSend_Weak()
    {
        using (Radio.Open(this.simpleService1, true))
        {
            Radio.Send<ISimpleService>().Test(1);
        }
    }

    [Benchmark]
    public void OpenSend8_Weak()
    {
        using (Radio.Open(this.simpleService1, true))
        {
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(1);
        }

        return;
    }

    [Benchmark]
    public void SendKey()
    {
        Radio.SendWithKey<ISimpleService, int>(1).Test(1);
    }

    [Benchmark]
    public void OpenSend_Key()
    {
        using (Radio.OpenWithKey<ISimpleService, int>(this.simpleService1, 1))
        {
            Radio.SendWithKey<ISimpleService, int>(1).Test(1);
        }
    }

    /*[Benchmark]
    public void OpenSend8_Key()
    {
        using (Radio.OpenWithKey<ISimpleService, int>(this.simpleService1, 1))
        {
            Radio.SendWithKey<ISimpleService, int>(1).Test(1);
            Radio.SendWithKey<ISimpleService, int>(1).Test(1);
            Radio.SendWithKey<ISimpleService, int>(1).Test(1);
            Radio.SendWithKey<ISimpleService, int>(1).Test(1);
            Radio.SendWithKey<ISimpleService, int>(1).Test(1);
            Radio.SendWithKey<ISimpleService, int>(1).Test(1);
            Radio.SendWithKey<ISimpleService, int>(1).Test(1);
            Radio.SendWithKey<ISimpleService, int>(1).Test(1);
        }
    }

    [Benchmark]
    public void Class_Send()
    {
        this.TestRadio.Send<ISimpleService>().Test(1);
        return;
    }

    [Benchmark]
    public void Class_OpenSend()
    {
        using (this.TestRadio.Open(this.simpleService1))
        {
            this.TestRadio.Send<ISimpleService>().Test(1);
        }

        return;
    }

    [Benchmark]
    public void Class_OpenSend8()
    {
        using (this.TestRadio.Open(this.simpleService1))
        {
            this.TestRadio.Send<ISimpleService>().Test(1);
            this.TestRadio.Send<ISimpleService>().Test(1);
            this.TestRadio.Send<ISimpleService>().Test(1);
            this.TestRadio.Send<ISimpleService>().Test(1);
            this.TestRadio.Send<ISimpleService>().Test(1);
            this.TestRadio.Send<ISimpleService>().Test(1);
            this.TestRadio.Send<ISimpleService>().Test(1);
            this.TestRadio.Send<ISimpleService>().Test(1);
        }

        return;
    }

    [Benchmark]
    public void Class_OpenSend_Weak()
    {
        using (this.TestRadio.Open(this.simpleService1, true))
        {
            this.TestRadio.Send<ISimpleService>().Test(1);
        }
    }

    [Benchmark]
    public void Class_OpenSend8_Weak()
    {
        using (this.TestRadio.Open(this.simpleService1, true))
        {
            this.TestRadio.Send<ISimpleService>().Test(1);
            this.TestRadio.Send<ISimpleService>().Test(1);
            this.TestRadio.Send<ISimpleService>().Test(1);
            this.TestRadio.Send<ISimpleService>().Test(1);
            this.TestRadio.Send<ISimpleService>().Test(1);
            this.TestRadio.Send<ISimpleService>().Test(1);
            this.TestRadio.Send<ISimpleService>().Test(1);
            this.TestRadio.Send<ISimpleService>().Test(1);
        }

        return;
    }

    [Benchmark]
    public void Class_SendKey()
    {
        this.TestRadio.SendWithKey<ISimpleService, int>(1).Test(1);
    }

    [Benchmark]
    public void Class_OpenSend_Key()
    {
        using (this.TestRadio.OpenWithKey<ISimpleService, int>(this.simpleService1, 1))
        {
            this.TestRadio.SendWithKey<ISimpleService, int>(1).Test(1);
        }
    }

    [Benchmark]
    public void Class_OpenSend8_Key()
    {
        using (this.TestRadio.OpenWithKey<ISimpleService, int>(this.simpleService1, 1))
        {
            this.TestRadio.SendWithKey<ISimpleService, int>(1).Test(1);
            this.TestRadio.SendWithKey<ISimpleService, int>(1).Test(1);
            this.TestRadio.SendWithKey<ISimpleService, int>(1).Test(1);
            this.TestRadio.SendWithKey<ISimpleService, int>(1).Test(1);
            this.TestRadio.SendWithKey<ISimpleService, int>(1).Test(1);
            this.TestRadio.SendWithKey<ISimpleService, int>(1).Test(1);
            this.TestRadio.SendWithKey<ISimpleService, int>(1).Test(1);
            this.TestRadio.SendWithKey<ISimpleService, int>(1).Test(1);
        }
    }*/
}
