// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using BenchmarkDotNet.Attributes;
using CrossChannel;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using PubSub;

namespace Benchmark;

[RadioServiceInterface]
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

[RadioServiceInterface]
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

public class MpHandler1 : IRequestHandler<int, int>
{
    public int Invoke(int request) => request * 1;
}

public class MpHandler2 : IRequestHandler<int, int>
{
    public int Invoke(int request) => request * 2;
}

public class MpHandler3 : IRequestHandler<int, int>
{
    public int Invoke(int request) => request * 3;
}

public class MpHandler4 : IRequestHandler<int, int>
{
    public int Invoke(int request) => request * 4;
}

public class MpHandler5 : IRequestHandler<int, int>
{
    public int Invoke(int request) => request * 5;
}

public class MpHandler6 : IRequestHandler<int, int>
{
    public int Invoke(int request) => request * 6;
}

public class MpHandler7 : IRequestHandler<int, int>
{
    public int Invoke(int request) => request * 7;
}

public class MpHandler8 : IRequestHandler<int, int>
{
    public int Invoke(int request) => request * 8;
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

    private ISimpleServiceB simpleServiceB1 = new SimpleServiceB(1);
    private ISimpleServiceB simpleServiceB2 = new SimpleServiceB(2);
    private ISimpleServiceB simpleServiceB3 = new SimpleServiceB(3);
    private ISimpleServiceB simpleServiceB4 = new SimpleServiceB(4);
    private ISimpleServiceB simpleServiceB5 = new SimpleServiceB(5);
    private ISimpleServiceB simpleServiceB6 = new SimpleServiceB(6);
    private ISimpleServiceB simpleServiceB7 = new SimpleServiceB(7);
    private ISimpleServiceB simpleServiceB8 = new SimpleServiceB(8);

    public H2HBenchmark()
    {
        var sc = new ServiceCollection();
        sc.AddMessagePipe();
        this.Provider = sc.BuildServiceProvider();
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    // [Benchmark]
    public void CC_OpenSend()
    {
        using (Radio.Open<int>(x => { }))
        {
            Radio.Send<int>(1);
        }

        return;
    }

    // [Benchmark]
    public void CC_OpenSend8()
    {
        using (Radio.Open<int>(x => { }))
        {
            Radio.Send<int>(1);
            Radio.Send<int>(2);
            Radio.Send<int>(3);
            Radio.Send<int>(4);
            Radio.Send<int>(5);
            Radio.Send<int>(6);
            Radio.Send<int>(7);
            Radio.Send<int>(8);
        }

        return;
    }

    [Benchmark]
    public void CC_OpenSend88()
    {
        using (Radio.Open<int>(x => { }))
        using (Radio.Open<int>(x => { }))
        using (Radio.Open<int>(x => { }))
        using (Radio.Open<int>(x => { }))
        using (Radio.Open<int>(x => { }))
        using (Radio.Open<int>(x => { }))
        using (Radio.Open<int>(x => { }))
        using (Radio.Open<int>(x => { }))
        {
            Radio.Send<int>(1);
            Radio.Send<int>(2);
            Radio.Send<int>(3);
            Radio.Send<int>(4);
            Radio.Send<int>(5);
            Radio.Send<int>(6);
            Radio.Send<int>(7);
            Radio.Send<int>(8);
        }

        return;
    }

    // [Benchmark]
    public void CC2_OpenSend()
    {
        using (NewRadio.Open(this.simpleService1))
        {
            NewRadio.Send<ISimpleService>().Test(1);
        }

        return;
    }

    // [Benchmark]
    public void CC2_OpenSend8()
    {
        using (NewRadio.Open(this.simpleService1))
        {
            NewRadio.Send<ISimpleService>().Test(1);
            NewRadio.Send<ISimpleService>().Test(2);
            NewRadio.Send<ISimpleService>().Test(3);
            NewRadio.Send<ISimpleService>().Test(4);
            NewRadio.Send<ISimpleService>().Test(5);
            NewRadio.Send<ISimpleService>().Test(6);
            NewRadio.Send<ISimpleService>().Test(7);
            NewRadio.Send<ISimpleService>().Test(8);
        }

        return;
    }

    [Benchmark]
    public void CC2_OpenSend88()
    {
        using (NewRadio.Open(this.simpleService1))
        using (NewRadio.Open(this.simpleService2))
        using (NewRadio.Open(this.simpleService3))
        using (NewRadio.Open(this.simpleService4))
        using (NewRadio.Open(this.simpleService5))
        using (NewRadio.Open(this.simpleService6))
        using (NewRadio.Open(this.simpleService7))
        using (NewRadio.Open(this.simpleService8))
        {
            NewRadio.Send<ISimpleService>().Test(1);
            NewRadio.Send<ISimpleService>().Test(2);
            NewRadio.Send<ISimpleService>().Test(3);
            NewRadio.Send<ISimpleService>().Test(4);
            NewRadio.Send<ISimpleService>().Test(5);
            NewRadio.Send<ISimpleService>().Test(6);
            NewRadio.Send<ISimpleService>().Test(7);
            NewRadio.Send<ISimpleService>().Test(8);
        }

        return;
    }

    // [Benchmark]
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

    // [Benchmark]
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

    /*[Benchmark]
    public void PS_OpenSend()
    {
        Hub.Default.Subscribe<int>(x => { });
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
    }*/

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
