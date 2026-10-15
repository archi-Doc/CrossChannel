// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace XUnitTest;

[Collection(StaticRadioCollection.Name)]
public class DependencyInjectionTest
{
    private readonly record struct DiKey(int Value);

    [Fact]
    public void LocalRadio()
    {
        var services = new ServiceCollection();
        services.AddCrossChannel();

        // ISender<TService> must be registered exactly once per service.
        services.Count(x => x.ServiceType == typeof(ISender<ITestService>)).Is(1);
        services.Count(x => x.ServiceType == typeof(IChannel<ITestService>)).Is(1);
        services.Count(x => x.ServiceType == typeof(ITestService)).Is(1);

        var provider = services.BuildServiceProvider();
        var channel = provider.GetRequiredService<IChannel<ITestService>>();
        var sender = provider.GetRequiredService<ISender<ITestService>>();
        provider.GetServices<ISender<ITestService>>().Count().Is(1);

        sender.Send().Double(1).IsEmpty.IsTrue();

        using (channel.Open(new TestService()))
        {
            sender.Send().Double(1).SequenceEqual([2,]).IsTrue();
            provider.GetRequiredService<ITestService>().Double(2).SequenceEqual([4,]).IsTrue();
        }

        sender.Send().Double(1).IsEmpty.IsTrue();
    }

    [Fact]
    public void StaticRadio()
    {
        var services = new ServiceCollection();
        services.AddCrossChannel(false);

        services.Count(x => x.ServiceType == typeof(ISender<ITestService>)).Is(1);

        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender<ITestService>>();

        using (Radio.Open<ITestService>(new TestService()))
        {
            sender.Send().Double(1).SequenceEqual([2,]).IsTrue();
        }
    }

    [Fact]
    public void StaticRadioSenderWithKey()
    {// DiKey is private to this class, so the keyed channels do not interfere with other static Radio tests.
        var services = new ServiceCollection();
        services.AddCrossChannel(false);
        var sender = services.BuildServiceProvider().GetRequiredService<ISender<ITestService>>();

        sender.SendWithKey(new DiKey(1)).Double(1).IsEmpty.IsTrue();
        using (Radio.OpenWithKey<ITestService, DiKey>(new TestService(), new DiKey(1)))
        {
            sender.SendWithKey(new DiKey(1)).Double(2).SequenceEqual([4,]).IsTrue();
            sender.SendWithKey(new DiKey(2)).Double(2).IsEmpty.IsTrue();
            sender.Send().Double(2).IsEmpty.IsTrue(); // The keyless channel is a different one.
        }

        sender.SendWithKey(new DiKey(1)).Double(1).IsEmpty.IsTrue();
        Radio.TryGetChannelWithKey<ITestService, DiKey>(new DiKey(1), out _).IsFalse();
    }

    [Fact]
    public void LocalRadioSenderUsesTheBrokerOfTheChannel()
    {// The sender caches the broker, which is the broker of the channel of the LocalRadio.
        var services = new ServiceCollection();
        services.AddCrossChannel();
        var provider = services.BuildServiceProvider();
        var radio = provider.GetRequiredService<LocalRadio>();
        var sender = provider.GetRequiredService<ISender<ITestService>>();

        ReferenceEquals(sender.Send(), radio.GetChannel<ITestService>().GetBroker()).IsTrue();
        ReferenceEquals(sender.Send(), sender.Send()).IsTrue();
        ReferenceEquals(sender.Send(), provider.GetRequiredService<ITestService>()).IsTrue();

        using (radio.Open<ITestService>(new TestService()))
        {
            sender.Send().Double(3).SequenceEqual([6,]).IsTrue();
        }

        // SendWithKey still looks up the keyed channel through the radio.
        using (radio.OpenWithKey<ITestService, DiKey>(new TestService(), new DiKey(1)))
        {
            sender.SendWithKey(new DiKey(1)).Double(4).SequenceEqual([8,]).IsTrue();
            sender.Send().Double(4).IsEmpty.IsTrue();
        }
    }

    [Fact]
    public void EachProviderOwnsItsLocalRadio()
    {
        var services1 = new ServiceCollection();
        services1.AddCrossChannel();
        var provider1 = services1.BuildServiceProvider();

        var services2 = new ServiceCollection();
        services2.AddCrossChannel();
        var provider2 = services2.BuildServiceProvider();

        ReferenceEquals(provider1.GetRequiredService<LocalRadio>(), provider2.GetRequiredService<LocalRadio>()).IsFalse();

        using (provider1.GetRequiredService<IChannel<ITestService>>().Open(new TestService()))
        {
            provider1.GetRequiredService<ISender<ITestService>>().Send().Double(1).SequenceEqual([2,]).IsTrue();
            provider2.GetRequiredService<ISender<ITestService>>().Send().Double(1).IsEmpty.IsTrue();
        }
    }

    [Fact]
    public void AddCrossChannelTwiceRegistersEachServiceOnce()
    {
        // Registrations are only ever added, so this snapshot is a subset of the ones AddCrossChannel enumerates.
        var registrations = RadioServiceRegistry.Registrations.ToArray();
        foreach (var useLocalRadio in new[] { true, false, })
        {
            var services = new ServiceCollection();
            services.AddCrossChannel(useLocalRadio);
            services.AddCrossChannel(useLocalRadio);

            services.Count(x => x.ServiceType == typeof(LocalRadio)).Is(useLocalRadio ? 1 : 0);
            foreach (var x in registrations)
            {
                var expected = x.AutoRegisterServiceAndSender ? 1 : 0;
                services.Count(y => y.ServiceType == typeof(IChannel<>).MakeGenericType(x.ServiceType)).Is(1);
                services.Count(y => y.ServiceType == typeof(ISender<>).MakeGenericType(x.ServiceType)).Is(expected);
                services.Count(y => y.ServiceType == x.ServiceType).Is(expected);
            }

            var provider = services.BuildServiceProvider();
            provider.GetServices<ISender<ITestService>>().Count().Is(1);
            provider.GetServices<ITestService>().Count().Is(1);
        }
    }

    [Fact]
    public void PreRegisteredServicesAreKept()
    {
        var radio = new LocalRadio();
        var service = new TestService();
        var services = new ServiceCollection();
        services.AddSingleton(radio);
        services.AddSingleton<IVoidService>(new VoidService());
        services.AddCrossChannel();

        services.Count(x => x.ServiceType == typeof(LocalRadio)).Is(1);
        services.Count(x => x.ServiceType == typeof(IVoidService)).Is(1);

        var provider = services.BuildServiceProvider();
        ReferenceEquals(provider.GetRequiredService<LocalRadio>(), radio).IsTrue();
        ReferenceEquals(provider.GetRequiredService<IChannel<ITestService>>(), radio.GetChannel<ITestService>()).IsTrue();
        provider.GetRequiredService<IVoidService>().IsInstanceOf<VoidService>(); // Not replaced by the broker.

        using (radio.Open<ITestService>(service))
        {
            provider.GetRequiredService<ISender<ITestService>>().Send().Double(2).SequenceEqual([4,]).IsTrue();
            provider.GetRequiredService<ITestService>().Double(3).SequenceEqual([6,]).IsTrue();
        }
    }
}
