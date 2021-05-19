// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using CrossChannel;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using PubSub;

#pragma warning disable SA1649 // File name should match first type name

namespace Benchmark
{
    [Config(typeof(BenchmarkConfig))]
    public class H2HBenchmark
    {
        public ServiceProvider Provider { get; }

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
        public void MP_OpenSend()
        {
            var sub = this.Provider.GetService<ISubscriber<int>>()!;
            var pub = this.Provider.GetService<IPublisher<int>>()!;
            using (var i = sub.Subscribe(x => { }))
            {
                pub.Publish(3);
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
        public void PS_OpenSend()
        {
            Hub.Default.Subscribe<int>(x => { });
            Hub.Default.Publish<int>(3);
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
        }

        /*[Benchmark]
        public void CCO_OpenSend()
        {
            using (var c = Arc.CrossChannel.Obsolete.CrossChannel.Open<int>(this, x => { }))
            {
                Arc.CrossChannel.Obsolete.CrossChannel.Send<int>(1);
            }

            return;
        }

        [Benchmark]
        public void CCO_OpenSend8()
        {
            using (var c = Arc.CrossChannel.Obsolete.CrossChannel.Open<int>(this, x => { }))
            {
                Arc.CrossChannel.Obsolete.CrossChannel.Send<int>(1);
                Arc.CrossChannel.Obsolete.CrossChannel.Send<int>(2);
                Arc.CrossChannel.Obsolete.CrossChannel.Send<int>(3);
                Arc.CrossChannel.Obsolete.CrossChannel.Send<int>(4);
                Arc.CrossChannel.Obsolete.CrossChannel.Send<int>(5);
                Arc.CrossChannel.Obsolete.CrossChannel.Send<int>(6);
                Arc.CrossChannel.Obsolete.CrossChannel.Send<int>(7);
                Arc.CrossChannel.Obsolete.CrossChannel.Send<int>(8);
            }

            return;
        }*/
    }
}
