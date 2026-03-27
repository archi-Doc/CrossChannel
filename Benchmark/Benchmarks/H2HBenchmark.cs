// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using BenchmarkDotNet.Attributes;
using CrossChannel;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using PubSub;

namespace Benchmark;

[RadioService]
public interface ISimpleService : IRadioService
{
    void Test(int x);
}

public class SimpleService : ISimpleService
{
    public void Test(int x)
    {
    }
}

[RadioService]
public interface ISimpleServiceB : IRadioService
{
    RadioResult<int> Test(int x);
}

public class SimpleServiceB : ISimpleServiceB
{
    private readonly int m;

    public SimpleServiceB(int m)
    {
        this.m = m;
    }

    public RadioResult<int> Test(int x)
    {
        return new(x * this.m);
    }
}

[Config(typeof(BenchmarkConfig))]
public class H2HBenchmark
{
    public ServiceProvider Provider { get; }

    private ISimpleService simpleService1 = new SimpleService();
    private ISimpleService simpleService2 = new SimpleService();
    private ISimpleService simpleService3 = new SimpleService();
    private ISimpleService simpleService4 = new SimpleService();
    private ISimpleService simpleService5 = new SimpleService();
    private ISimpleService simpleService6 = new SimpleService();
    private ISimpleService simpleService7 = new SimpleService();
    private ISimpleService simpleService8 = new SimpleService();

    private readonly RadioClass radio;

    public H2HBenchmark()
    {
        var sc = new ServiceCollection();
        sc.AddMessagePipe();
        this.Provider = sc.BuildServiceProvider();
        this.radio = new RadioClass();
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public void CC_OpenSend()
    {
        using (Radio.Open(this.simpleService1))
        {
            Radio.Send<ISimpleService>().Test(1);
        }

        return;
    }

    [Benchmark]
    public void CC_OpenSend8()
    {
        using (Radio.Open(this.simpleService1))
        {
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(2);
            Radio.Send<ISimpleService>().Test(3);
            Radio.Send<ISimpleService>().Test(4);
            Radio.Send<ISimpleService>().Test(5);
            Radio.Send<ISimpleService>().Test(6);
            Radio.Send<ISimpleService>().Test(7);
            Radio.Send<ISimpleService>().Test(8);
        }

        return;
    }

    [Benchmark]
    public void CC_OpenSend88()
    {
        using (Radio.Open(this.simpleService1))
        using (Radio.Open(this.simpleService2))
        using (Radio.Open(this.simpleService3))
        using (Radio.Open(this.simpleService4))
        using (Radio.Open(this.simpleService5))
        using (Radio.Open(this.simpleService6))
        using (Radio.Open(this.simpleService7))
        using (Radio.Open(this.simpleService8))
        {
            Radio.Send<ISimpleService>().Test(1);
            Radio.Send<ISimpleService>().Test(2);
            Radio.Send<ISimpleService>().Test(3);
            Radio.Send<ISimpleService>().Test(4);
            Radio.Send<ISimpleService>().Test(5);
            Radio.Send<ISimpleService>().Test(6);
            Radio.Send<ISimpleService>().Test(7);
            Radio.Send<ISimpleService>().Test(8);
        }

        return;
    }

    [Benchmark]
    public void CC2_OpenSend()
    {
        using (this.radio.Open(this.simpleService1))
        {
            this.radio.Send<ISimpleService>().Test(1);
        }

        return;
    }

    [Benchmark]
    public void CC2_OpenSend8()
    {
        using (this.radio.Open(this.simpleService1))
        {
            this.radio.Send<ISimpleService>().Test(1);
            this.radio.Send<ISimpleService>().Test(2);
            this.radio.Send<ISimpleService>().Test(3);
            this.radio.Send<ISimpleService>().Test(4);
            this.radio.Send<ISimpleService>().Test(5);
            this.radio.Send<ISimpleService>().Test(6);
            this.radio.Send<ISimpleService>().Test(7);
            this.radio.Send<ISimpleService>().Test(8);
        }

        return;
    }

    [Benchmark]
    public void CC2_OpenSend88()
    {
        using (this.radio.Open(this.simpleService1))
        using (this.radio.Open(this.simpleService2))
        using (this.radio.Open(this.simpleService3))
        using (this.radio.Open(this.simpleService4))
        using (this.radio.Open(this.simpleService5))
        using (this.radio.Open(this.simpleService6))
        using (this.radio.Open(this.simpleService7))
        using (this.radio.Open(this.simpleService8))
        {
            this.radio.Send<ISimpleService>().Test(1);
            this.radio.Send<ISimpleService>().Test(2);
            this.radio.Send<ISimpleService>().Test(3);
            this.radio.Send<ISimpleService>().Test(4);
            this.radio.Send<ISimpleService>().Test(5);
            this.radio.Send<ISimpleService>().Test(6);
            this.radio.Send<ISimpleService>().Test(7);
            this.radio.Send<ISimpleService>().Test(8);
        }

        return;
    }

    [Benchmark]
    public void MP_OpenSend()
    {
        var sub = this.Provider.GetService<ISubscriber<int>>()!;
        var pub = this.Provider.GetService<IPublisher<int>>()!;
        using (var i = sub.Subscribe(x => { }))
        {
            pub.Publish(1);
        }

        return;
    }

    [Benchmark]
    public void MP_OpenSend8()
    {
        var sub = this.Provider.GetService<ISubscriber<int>>()!;
        var pub = this.Provider.GetService<IPublisher<int>>()!;
        using (var i = sub.Subscribe(x => { }))
        {
            pub.Publish(1);
            pub.Publish(2);
            pub.Publish(3);
            pub.Publish(4);
            pub.Publish(5);
            pub.Publish(6);
            pub.Publish(7);
            pub.Publish(8);
        }

        return;
    }

    [Benchmark]
    public void MP_OpenSend88()
    {
        var sub = this.Provider.GetService<ISubscriber<int>>()!;
        var pub = this.Provider.GetService<IPublisher<int>>()!;
        using (sub.Subscribe(x => { }))
        using (sub.Subscribe(x => { }))
        using (sub.Subscribe(x => { }))
        using (sub.Subscribe(x => { }))
        using (sub.Subscribe(x => { }))
        using (sub.Subscribe(x => { }))
        using (sub.Subscribe(x => { }))
        using (sub.Subscribe(x => { }))
        {
            pub.Publish(1);
            pub.Publish(2);
            pub.Publish(3);
            pub.Publish(4);
            pub.Publish(5);
            pub.Publish(6);
            pub.Publish(7);
            pub.Publish(8);
        }

        return;
    }

    [Benchmark]
    public void PS_OpenSend()
    {
        Hub.Default.Subscribe<int>( x => { });
        Hub.Default.Publish<int>(1);
        Hub.Default.Unsubscribe<int>();

        return;
    }

    [Benchmark]
    public void PS_OpenSend8()
    {
        Hub.Default.Subscribe<int>(x => { });
        Hub.Default.Publish<int>(1);
        Hub.Default.Publish<int>(2);
        Hub.Default.Publish<int>(3);
        Hub.Default.Publish<int>(4);
        Hub.Default.Publish<int>(5);
        Hub.Default.Publish<int>(6);
        Hub.Default.Publish<int>(7);
        Hub.Default.Publish<int>(8);
        Hub.Default.Unsubscribe<int>();

        return;
    }

    [Benchmark]
    public void PS_OpenSend88()
    {
        Hub.Default.Subscribe<int>(simpleService1, x => { });
        Hub.Default.Subscribe<int>(simpleService2, x => { });
        Hub.Default.Subscribe<int>(simpleService3, x => { });
        Hub.Default.Subscribe<int>(simpleService4, x => { });
        Hub.Default.Subscribe<int>(simpleService5, x => { });
        Hub.Default.Subscribe<int>(simpleService6, x => { });
        Hub.Default.Subscribe<int>(simpleService7, x => { });
        Hub.Default.Subscribe<int>(simpleService8, x => { });
        Hub.Default.Publish<int>(1);
        Hub.Default.Publish<int>(2);
        Hub.Default.Publish<int>(3);
        Hub.Default.Publish<int>(4);
        Hub.Default.Publish<int>(5);
        Hub.Default.Publish<int>(6);
        Hub.Default.Publish<int>(7);
        Hub.Default.Publish<int>(8);
        Hub.Default.Unsubscribe<int>(simpleService1);
        Hub.Default.Unsubscribe<int>(simpleService2);
        Hub.Default.Unsubscribe<int>(simpleService3);
        Hub.Default.Unsubscribe<int>(simpleService4);
        Hub.Default.Unsubscribe<int>(simpleService5);
        Hub.Default.Unsubscribe<int>(simpleService6);
        Hub.Default.Unsubscribe<int>(simpleService7);
        Hub.Default.Unsubscribe<int>(simpleService8);

        return;
    }

    /*[Benchmark]
    public void CC_OpenSend_Key()
    {
        using (var d = Radio.OpenKey<int, int>(3, x => { }))
        {
            Radio.SendKey<int, int>(3, 3);
        }

        return;
    }

    [Benchmark]
    public void CC_OpenSend8_Key()
    {
        using (var c = Radio.OpenKey<int, int>(3, x => { }))
        {
            Radio.SendKey<int, int>(3, 1);
            Radio.SendKey<int, int>(3, 2);
            Radio.SendKey<int, int>(3, 3);
            Radio.SendKey<int, int>(3, 4);
            Radio.SendKey<int, int>(3, 5);
            Radio.SendKey<int, int>(3, 6);
            Radio.SendKey<int, int>(3, 7);
            Radio.SendKey<int, int>(3, 8);
        }

        return;
    }

    [Benchmark]
    public void MP_OpenSend_Key()
    {
        var sub = this.Provider.GetService<ISubscriber<int, int>>()!;
        var pub = this.Provider.GetService<IPublisher<int, int>>()!;
        using (var i = sub.Subscribe(1, x => { }))
        {
            pub.Publish(1, 3);
        }

        return;
    }

    [Benchmark]
    public void MP_OpenSend8_Key()
    {
        var sub = this.Provider.GetService<ISubscriber<int, int>>()!;
        var pub = this.Provider.GetService<IPublisher<int, int>>()!;
        using (var i = sub.Subscribe(1, x => { }))
        {
            pub.Publish(1, 1);
            pub.Publish(1, 2);
            pub.Publish(1, 3);
            pub.Publish(1, 4);
            pub.Publish(1, 5);
            pub.Publish(1, 6);
            pub.Publish(1, 7);
            pub.Publish(1, 8);
        }

        return;
    }*/
}
