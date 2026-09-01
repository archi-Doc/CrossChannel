// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace XUnitTest;

public class DependencyInjectionTest
{
    [Fact]
    public void RadioClass()
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

        sender.Get().Double(1).IsEmpty.IsTrue();

        using (channel.Open(new TestService()))
        {
            sender.Get().Double(1).SequenceEqual([2,]).IsTrue();
            provider.GetRequiredService<ITestService>().Double(2).SequenceEqual([4,]).IsTrue();
        }

        sender.Get().Double(1).IsEmpty.IsTrue();
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
            sender.Get().Double(1).SequenceEqual([2,]).IsTrue();
        }
    }
}
