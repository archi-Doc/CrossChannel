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

public class RadioServiceRegistryTest
{
    [Fact]
    public void GetInformation()
    {
        var registration = RadioServiceRegistry.GetRegistration<ITestService>();
        registration.ServiceType.Is(typeof(ITestService));
        registration.MaxLinks.Is(int.MaxValue);
        registration.AutoRegisterServiceAndSender.IsTrue();

        // The generic and the Type-based overloads must return the same instance.
        ReferenceEquals(registration, RadioServiceRegistry.GetRegistration(typeof(ITestService))).IsTrue();
        ReferenceEquals(registration, RadioServiceRegistry.GetRegistration<ITestService>()).IsTrue();
    }

    [Fact]
    public void AttributeArguments()
    {
        RadioServiceRegistry.GetRegistration<ISingleService>().MaxLinks.Is(1);
        RadioServiceRegistry.GetRegistration<IConductorPresentationService>().MaxLinks.Is(1);
        RadioServiceRegistry.GetRegistration<IManualRegistrationService>().AutoRegisterServiceAndSender.IsFalse();
        RadioServiceRegistry.GetRegistration<IVoidService>().AutoRegisterServiceAndSender.IsTrue();
    }

    [Fact]
    public void GetUnregisteredType()
    {
        Assert.Throws<InvalidOperationException>(() => RadioServiceRegistry.GetRegistration(typeof(IDisposable)));
    }

    [Fact]
    public void RegisterDuplicate()
    {
        var registration = RadioServiceRegistry.GetRegistration<ITestService>();

        // A service type which is already registered must not be replaced.
        var result = RadioServiceRegistry.Register(new(typeof(ITestService), static x => throw new NotSupportedException(), static () => throw new NotSupportedException(), 12, false));
        result.IsFalse();

        ReferenceEquals(registration, RadioServiceRegistry.GetRegistration<ITestService>()).IsTrue();
        RadioServiceRegistry.GetRegistration<ITestService>().MaxLinks.Is(int.MaxValue);
    }

    [Fact]
    public void Channels()
    {
        var channels = RadioServiceRegistry.Registrations;

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
    public void ChannelFactoryAndBrokerFactory()
    {
        var registration = RadioServiceRegistry.GetRegistration<ITestService>();

        var channel = registration.ChannelFactory();
        channel.IsInstanceOf<Channel<ITestService>>();
        ReferenceEquals(channel, registration.ChannelFactory()).IsFalse();

        var broker = registration.BrokerFactory(channel);
        (broker is ITestService).IsTrue();
        ReferenceEquals(broker, channel.GetBroker()).IsFalse(); // A brand new broker instance.
    }

    [Fact]
    public void ServiceWithoutAutoRegistration()
    {// The generated broker must work even when the service is not registered in DI.
        var radio = new LocalRadio();
        var service = new ManualRegistrationService();

        using (radio.Open<IManualRegistrationService>(service))
        {
            radio.Send<IManualRegistrationService>().Test(3);
            service.Sum.Is(3);
        }
    }
}
