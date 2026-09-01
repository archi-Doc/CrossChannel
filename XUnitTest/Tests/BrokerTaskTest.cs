// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using CrossChannel;
using Xunit;

namespace XUnitTest;

[RadioService]
public interface IBrokerTaskService : IRadioService
{
    Task Increment();

    Task<RadioResult<int>> Get();
}

public class BrokerTaskService : IBrokerTaskService
{
    public int Count { get; private set; }

    private readonly int value;
    private readonly bool throwSynchronously;

    public BrokerTaskService(int value, bool throwSynchronously = false)
    {
        this.value = value;
        this.throwSynchronously = throwSynchronously;
    }

    Task IBrokerTaskService.Increment()
    {
        if (this.throwSynchronously)
        {
            throw new InvalidOperationException();
        }

        this.Count++;
        return Task.CompletedTask;
    }

    async Task<RadioResult<int>> IBrokerTaskService.Get()
    {
        if (this.throwSynchronously)
        {
            throw new InvalidOperationException();
        }

        await Task.Yield();
        return this.value == 0 ? default : new(this.value);
    }
}

public class BrokerTaskTest
{
    [Fact]
    public async Task NoReceiver()
    {
        var radio = new RadioClass();

        await radio.Send<IBrokerTaskService>().Increment();
        (await radio.Send<IBrokerTaskService>().Get()).IsEmpty.IsTrue();
    }

    [Fact]
    public async Task SingleReceiver()
    {
        var radio = new RadioClass();
        var service = new BrokerTaskService(1);

        using (radio.Open<IBrokerTaskService>(service))
        {
            await radio.Send<IBrokerTaskService>().Increment();
            service.Count.Is(1);

            (await radio.Send<IBrokerTaskService>().Get()).SequenceEqual([1,]).IsTrue();
        }
    }

    [Fact]
    public async Task MultipleReceivers()
    {
        var radio = new RadioClass();
        var service1 = new BrokerTaskService(1);
        var service2 = new BrokerTaskService(0); // Returns an empty result.
        var service3 = new BrokerTaskService(2);

        using (radio.Open<IBrokerTaskService>(service1))
        using (radio.Open<IBrokerTaskService>(service2))
        using (radio.Open<IBrokerTaskService>(service3))
        {
            await radio.Send<IBrokerTaskService>().Increment();
            service1.Count.Is(1);
            service2.Count.Is(1);
            service3.Count.Is(1);

            (await radio.Send<IBrokerTaskService>().Get()).SequenceEqual([1, 2,]).IsTrue();
        }
    }

    [Fact]
    public async Task SynchronousExceptionIsCapturedInTheTask()
    {// The broker methods are not 'async', so a synchronous exception must not escape before the task is awaited.
        var radio = new RadioClass();

        using (radio.Open<IBrokerTaskService>(new BrokerTaskService(1, true)))
        {
            var task = radio.Send<IBrokerTaskService>().Increment();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);

            var task2 = radio.Send<IBrokerTaskService>().Get();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await task2);
        }
    }
}
