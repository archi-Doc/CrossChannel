// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using Xunit;

namespace XUnitTest;

[RadioService(AutoRegisterRadioServiceAndSender = false)]
public interface IManualRegistrationService : IRadioService
{
    void Test(int x);
}

public class ManualRegistrationService : IManualRegistrationService
{
    public int Sum { get; private set; }

    void IManualRegistrationService.Test(int x) => this.Sum += x;
}

public class ChannelRegistryTest
{
    [Fact]
    public void GetInformation()
    {
        var information = ChannelRegistry.Get<ITestService>();
        information.ServiceType.Is(typeof(ITestService));
        information.MaxLinks.Is(int.MaxValue);
        information.AutoRegisterRadioServiceAndSender.IsTrue();

        // The generic and the Type-based overloads must return the same instance.
        ReferenceEquals(information, ChannelRegistry.Get(typeof(ITestService))).IsTrue();
        ReferenceEquals(information, ChannelRegistry.Get<ITestService>()).IsTrue();
    }

    [Fact]
    public void AttributeArguments()
    {
        ChannelRegistry.Get<ISingleService>().MaxLinks.Is(1);
        ChannelRegistry.Get<IConductorPresentationService>().MaxLinks.Is(1);
        ChannelRegistry.Get<IManualRegistrationService>().AutoRegisterRadioServiceAndSender.IsFalse();
        ChannelRegistry.Get<IVoidService>().AutoRegisterRadioServiceAndSender.IsTrue();
    }

    [Fact]
    public void GetUnregisteredType()
    {
        Assert.Throws<InvalidOperationException>(() => ChannelRegistry.Get(typeof(IDisposable)));
    }

    [Fact]
    public void RegisterDuplicate()
    {
        var information = ChannelRegistry.Get<ITestService>();

        // A service type which is already registered must not be replaced.
        var result = ChannelRegistry.Register(new(typeof(ITestService), static x => throw new NotSupportedException(), static () => throw new NotSupportedException(), 12, false));
        result.IsFalse();

        ReferenceEquals(information, ChannelRegistry.Get<ITestService>()).IsTrue();
        ChannelRegistry.Get<ITestService>().MaxLinks.Is(int.MaxValue);
    }

    [Fact]
    public void Channels()
    {
        var channels = ChannelRegistry.Channels;

        // Every service declared in this assembly must be registered by the module initializer.
        channels.Any(x => x.ServiceType == typeof(ITestService)).IsTrue();
        channels.Any(x => x.ServiceType == typeof(ISingleService)).IsTrue();
        channels.Any(x => x.ServiceType == typeof(IVoidService)).IsTrue();
        channels.Any(x => x.ServiceType == typeof(IResultService)).IsTrue();
        channels.Any(x => x.ServiceType == typeof(IAsyncService)).IsTrue();
        channels.Any(x => x.ServiceType == typeof(IManualRegistrationService)).IsTrue();
        channels.Any(x => x.ServiceType == typeof(IConductorPresentationService)).IsTrue(); // Global namespace.

        // The service type must be registered exactly once.
        channels.Count(x => x.ServiceType == typeof(ITestService)).Is(1);
    }

    [Fact]
    public void NewChannelAndNewBroker()
    {
        var information = ChannelRegistry.Get<ITestService>();

        var channel = information.NewChannel();
        channel.IsInstanceOf<Channel<ITestService>>();
        ReferenceEquals(channel, information.NewChannel()).IsFalse();

        var broker = information.NewBroker(channel);
        (broker is ITestService).IsTrue();
        ReferenceEquals(broker, channel.GetBroker()).IsFalse(); // A brand new broker instance.
    }

    [Fact]
    public void ServiceWithoutAutoRegistration()
    {// The generated broker must work even when the service is not registered in DI.
        var radio = new RadioClass();
        var service = new ManualRegistrationService();

        using (radio.Open<IManualRegistrationService>(service))
        {
            radio.Send<IManualRegistrationService>().Test(3);
            service.Sum.Is(3);
        }
    }
}
