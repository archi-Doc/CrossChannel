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

    private ISimpleService simpleService = new SimpleService();

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

    [Benchmark]
    public void CC_OpenSend()
    {
        using (var c = Radio.Open<int>(x => { }))
        {
            Radio.Send<int>(1);
        }

        return;
    }

    [Benchmark]
    public void CC_OpenSend8()
    {
        using (var c = Radio.Open<int>(x => { }))
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
    public int CC_Complex()
    {
        int total = 0;
        using (Radio.OpenTwoWay<int, int>(x => x * 1))
        using (Radio.OpenTwoWay<int, int>(x => x * 2))
        using (Radio.OpenTwoWay<int, int>(x => x * 3))
        using (Radio.OpenTwoWay<int, int>(x => x * 4))
        using (Radio.OpenTwoWay<int, int>(x => x * 5))
        using (Radio.OpenTwoWay<int, int>(x => x * 6))
        using (Radio.OpenTwoWay<int, int>(x => x * 7))
        using (Radio.OpenTwoWay<int, int>(x => x * 8))
        {
            total += Radio.SendTwoWay<int, int>(1).Sum();
            total += Radio.SendTwoWay<int, int>(2).Sum();
            total += Radio.SendTwoWay<int, int>(3).Sum();
            total += Radio.SendTwoWay<int, int>(4).Sum();
            total += Radio.SendTwoWay<int, int>(5).Sum();
            total += Radio.SendTwoWay<int, int>(6).Sum();
            total += Radio.SendTwoWay<int, int>(7).Sum();
            total += Radio.SendTwoWay<int, int>(8).Sum();
        }

        return total;
    }

    [Benchmark]
    public void CC2_OpenSend()
    {
        using (NewRadio.Open(this.simpleService))
        {
            NewRadio.Send<ISimpleService>().Test(1);
        }

        return;
    }

    [Benchmark]
    public void CC2_OpenSend8()
    {
        using (NewRadio.Open(this.simpleService))
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
    public int CC2_Complex()
    {
        int total = 0;
        using (NewRadio.Open(this.simpleServiceB1))
        using (NewRadio.Open(this.simpleServiceB2))
        using (NewRadio.Open(this.simpleServiceB3))
        using (NewRadio.Open(this.simpleServiceB4))
        using (NewRadio.Open(this.simpleServiceB5))
        using (NewRadio.Open(this.simpleServiceB6))
        using (NewRadio.Open(this.simpleServiceB7))
        using (NewRadio.Open(this.simpleServiceB8))
        {
            total += NewRadio.Send<ISimpleServiceB>().Test(1).Sum();
            total += NewRadio.Send<ISimpleServiceB>().Test(2).Sum();
            total += NewRadio.Send<ISimpleServiceB>().Test(3).Sum();
            total += NewRadio.Send<ISimpleServiceB>().Test(4).Sum();
            total += NewRadio.Send<ISimpleServiceB>().Test(5).Sum();
            total += NewRadio.Send<ISimpleServiceB>().Test(6).Sum();
            total += NewRadio.Send<ISimpleServiceB>().Test(7).Sum();
            total += NewRadio.Send<ISimpleServiceB>().Test(8).Sum();
        }

        return total;
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
    public int MP_Complex()
    {
        int total = 0;
        this.Provider.GetService<MpHandler1>();
        this.Provider.GetService<MpHandler2>();
        this.Provider.GetService<MpHandler3>();
        this.Provider.GetService<MpHandler4>();
        this.Provider.GetService<MpHandler5>();
        this.Provider.GetService<MpHandler6>();
        this.Provider.GetService<MpHandler7>();
        this.Provider.GetService<MpHandler8>();
        var handler = this.Provider.GetService<IRequestAllHandler<int, int>>()!;
        {
            total += handler.InvokeAll(1).Sum();
            total += handler.InvokeAll(2).Sum();
            total += handler.InvokeAll(3).Sum();
            total += handler.InvokeAll(4).Sum();
            total += handler.InvokeAll(5).Sum();
            total += handler.InvokeAll(6).Sum();
            total += handler.InvokeAll(7).Sum();
            total += handler.InvokeAll(8).Sum();
        }

        return total;
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
