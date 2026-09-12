// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using Xunit;

namespace XUnitTest;

[RadioService(AutoRegisterServiceAndSender = false)]
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
        var registration = ChannelRegistry.GetRegistration<ITestService>();
        registration.ServiceType.Is(typeof(ITestService));
        registration.MaxLinks.Is(int.MaxValue);
        registration.AutoRegisterServiceAndSender.IsTrue();

        // The generic and the Type-based overloads must return the same instance.
        ReferenceEquals(registration, ChannelRegistry.GetRegistration(typeof(ITestService))).IsTrue();
        ReferenceEquals(registration, ChannelRegistry.GetRegistration<ITestService>()).IsTrue();
    }

    [Fact]
    public void AttributeArguments()
    {
        ChannelRegistry.GetRegistration<ISingleService>().MaxLinks.Is(1);
        ChannelRegistry.GetRegistration<IConductorPresentationService>().MaxLinks.Is(1);
        ChannelRegistry.GetRegistration<IManualRegistrationService>().AutoRegisterServiceAndSender.IsFalse();
        ChannelRegistry.GetRegistration<IVoidService>().AutoRegisterServiceAndSender.IsTrue();
    }

    [Fact]
    public void GetUnregisteredType()
    {
        Assert.Throws<InvalidOperationException>(() => ChannelRegistry.GetRegistration(typeof(IDisposable)));
    }

    [Fact]
    public void RegisterDuplicate()
    {
        var registration = ChannelRegistry.GetRegistration<ITestService>();

        // A service type which is already registered must not be replaced.
        var result = ChannelRegistry.Register(new(typeof(ITestService), static x => throw new NotSupportedException(), static () => throw new NotSupportedException(), 12, false));
        result.IsFalse();

        ReferenceEquals(registration, ChannelRegistry.GetRegistration<ITestService>()).IsTrue();
        ChannelRegistry.GetRegistration<ITestService>().MaxLinks.Is(int.MaxValue);
    }

    [Fact]
    public void Channels()
    {
        var channels = ChannelRegistry.Registrations;

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
    public void CreateChannelAndCreateBroker()
    {
        var registration = ChannelRegistry.GetRegistration<ITestService>();

        var channel = registration.CreateChannel();
        channel.IsInstanceOf<Channel<ITestService>>();
        ReferenceEquals(channel, registration.CreateChannel()).IsFalse();

        var broker = registration.CreateBroker(channel);
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
