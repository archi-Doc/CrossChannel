// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace XUnitTest;

public class ServiceRegistrationTest
{
    [Fact]
    public void EveryRegisteredChannelIsAdded()
    {
        var services = new ServiceCollection();
        services.AddCrossChannel();

        foreach (var x in ChannelRegistry.Registrations)
        {
            var channelType = typeof(IChannel<>).MakeGenericType(x.ServiceType);
            services.Count(y => y.ServiceType == channelType).Is(1);

            var senderType = typeof(ISender<>).MakeGenericType(x.ServiceType);
            var expected = x.AutoRegisterServiceAndSender ? 1 : 0;
            services.Count(y => y.ServiceType == senderType).Is(expected);
            services.Count(y => y.ServiceType == x.ServiceType).Is(expected);
        }
    }

    [Fact]
    public void AutoRegistrationCanBeDisabled()
    {
        var services = new ServiceCollection();
        services.AddCrossChannel();

        // IChannel<T> is always registered.
        services.Count(x => x.ServiceType == typeof(IChannel<IManualRegistrationService>)).Is(1);

        // The service and the sender are not, because AutoRegisterServiceAndSender is false.
        services.Count(x => x.ServiceType == typeof(ISender<IManualRegistrationService>)).Is(0);
        services.Count(x => x.ServiceType == typeof(IManualRegistrationService)).Is(0);

        var provider = services.BuildServiceProvider();
        provider.GetService<ISender<IManualRegistrationService>>().IsNull();
        provider.GetRequiredService<IChannel<IManualRegistrationService>>().IsNotNull();
    }

    [Fact]
    public void SenderAndChannelShareTheSameRadio()
    {
        var services = new ServiceCollection();
        services.AddCrossChannel();
        var provider = services.BuildServiceProvider();

        var radio = provider.GetRequiredService<RadioClass>();
        var channel = provider.GetRequiredService<IChannel<IVoidService>>();
        var sender = provider.GetRequiredService<ISender<IVoidService>>();

        ReferenceEquals(channel, radio.GetChannel<IVoidService>()).IsTrue();
        ReferenceEquals(sender.Send(), radio.Send<IVoidService>()).IsTrue();

        var service = new VoidService();
        using (channel.Open(service))
        {
            sender.Send().Add(3);
            provider.GetRequiredService<IVoidService>().Add(4);
            service.Sum.Is(7);
        }
    }

    [Fact]
    public void SenderWithKey()
    {
        var services = new ServiceCollection();
        services.AddCrossChannel();
        var provider = services.BuildServiceProvider();

        var radio = provider.GetRequiredService<RadioClass>();
        var sender = provider.GetRequiredService<ISender<ITestService>>();

        // An unknown key must return the broker of the empty channel.
        sender.SendWithKey(1).Double(1).IsEmpty.IsTrue();

        using (radio.OpenWithKey((ITestService)new TestService(), 1))
        {
            sender.SendWithKey(1).Double(2).SequenceEqual([4,]).IsTrue();
            sender.SendWithKey(2).Double(2).IsEmpty.IsTrue();
            sender.SendWithKey("1").Double(2).IsEmpty.IsTrue(); // A different key type.
            sender.Send().Double(2).IsEmpty.IsTrue(); // The keyless channel is a different one.
        }

        sender.SendWithKey(1).Double(2).IsEmpty.IsTrue();
    }

    [Fact]
    public void SingletonLifetime()
    {
        var services = new ServiceCollection();
        services.AddCrossChannel();
        var provider = services.BuildServiceProvider();

        ReferenceEquals(provider.GetRequiredService<RadioClass>(), provider.GetRequiredService<RadioClass>()).IsTrue();
        ReferenceEquals(provider.GetRequiredService<ISender<ITestService>>(), provider.GetRequiredService<ISender<ITestService>>()).IsTrue();
        ReferenceEquals(provider.GetRequiredService<IChannel<ITestService>>(), provider.GetRequiredService<IChannel<ITestService>>()).IsTrue();
    }

    [Fact]
    public void StaticRadioRegistration()
    {
        var services = new ServiceCollection();
        services.AddCrossChannel(false);
        var provider = services.BuildServiceProvider();

        // RadioClass is not registered when the static Radio is used.
        provider.GetService<RadioClass>().IsNull();

        var channel = provider.GetRequiredService<IChannel<ITestService>>();
        ReferenceEquals(channel, Radio.GetChannel<ITestService>()).IsTrue();

        var sender = provider.GetRequiredService<ISender<ITestService>>();
        ReferenceEquals(sender.Send(), Radio.Send<ITestService>()).IsTrue();
    }
}
