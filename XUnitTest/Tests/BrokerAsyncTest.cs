// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using CrossChannel;
using Xunit;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace XUnitTest;

[RadioService]
public interface IAsyncService : IRadioService
{
    Task Increment(int x);

    Task<RadioResult<int>> Value();

    Task<RadioResult<string?>> Text();
}

public class AsyncService : IAsyncService
{
    public enum Behavior
    {
        Normal,
        ThrowSynchronously,
        ThrowAsynchronously,
        Gate, // Completes when the gate is released.
    }

    public int Count { get; private set; }

    public int Sum { get; private set; }

    private readonly int value; // 0: returns an empty result.
    private readonly Behavior behavior;
    private readonly TaskCompletionSource gate = new();
    private readonly TaskCompletionSource<RadioResult<int>> valueGate = new();

    public AsyncService(int value, Behavior behavior = Behavior.Normal)
    {
        this.value = value;
        this.behavior = behavior;
    }

    public void ReleaseGate()
    {
        this.gate.TrySetResult();
        this.valueGate.TrySetResult(new(this.value));
    }

    Task IAsyncService.Increment(int x)
    {
        if (this.behavior == Behavior.ThrowSynchronously)
        {
            throw new InvalidOperationException();
        }
        else if (this.behavior == Behavior.Gate)
        {
            return this.gate.Task;
        }

        return this.IncrementAsync(x);
    }

    Task<RadioResult<int>> IAsyncService.Value()
    {
        if (this.behavior == Behavior.ThrowSynchronously)
        {
            throw new InvalidOperationException();
        }
        else if (this.behavior == Behavior.Gate)
        {
            return this.valueGate.Task;
        }

        return this.ValueAsync();
    }

    Task<RadioResult<string?>> IAsyncService.Text()
    {
        if (this.behavior == Behavior.ThrowSynchronously)
        {
            throw new InvalidOperationException();
        }

        return this.TextAsync();
    }

    private async Task IncrementAsync(int x)
    {
        await Task.Yield();
        if (this.behavior == Behavior.ThrowAsynchronously)
        {
            throw new InvalidOperationException();
        }

        this.Count++;
        this.Sum += x;
    }

    private async Task<RadioResult<int>> ValueAsync()
    {
        await Task.Yield();
        if (this.behavior == Behavior.ThrowAsynchronously)
        {
            throw new InvalidOperationException();
        }

        return this.value == 0 ? default : new(this.value);
    }

    private async Task<RadioResult<string?>> TextAsync()
    {
        await Task.Yield();
        if (this.value == 0)
        {
            return default;
        }

        return RadioResult<string?>.Single(this.value == -1 ? null : this.value.ToString());
    }
}

public class BrokerAsyncTest
{
    [Fact]
    public async Task NoReceiver()
    {
        var radio = new RadioClass();

        var task = radio.Send<IAsyncService>().Increment(1);
        task.IsCompletedSuccessfully.IsTrue(); // The fast path must not create a pending task.
        await task;

        var task2 = radio.Send<IAsyncService>().Value();
        task2.IsCompletedSuccessfully.IsTrue();
        (await task2).IsEmpty.IsTrue();
    }

    [Fact]
    public async Task SingleReceiver()
    {
        var radio = new RadioClass();
        var service = new AsyncService(3);

        using (radio.Open<IAsyncService>(service))
        {
            await radio.Send<IAsyncService>().Increment(5);
            service.Count.Is(1);
            service.Sum.Is(5);

            (await radio.Send<IAsyncService>().Value()).SequenceEqual([3,]).IsTrue();
        }
    }

    [Fact]
    public async Task SingleReceiverIsNotAwaitedByTheBroker()
    {// With a single receiver the task is passed through, so it must stay pending until the receiver completes.
        var radio = new RadioClass();
        var service = new AsyncService(8, AsyncService.Behavior.Gate);

        using (radio.Open<IAsyncService>(service))
        {
            var task = radio.Send<IAsyncService>().Increment(1);
            var task2 = radio.Send<IAsyncService>().Value();
            task.IsCompleted.IsFalse();
            task2.IsCompleted.IsFalse();

            service.ReleaseGate();
            await task;
            (await task2).SequenceEqual([8,]).IsTrue();
        }
    }

    [Fact]
    public async Task MultipleReceivers()
    {
        var radio = new RadioClass();
        var services = Enumerable.Range(1, 5).Select(x => new AsyncService(x)).ToArray();
        var links = services.Select(x => radio.Open<IAsyncService>(x)!).ToArray();

        await radio.Send<IAsyncService>().Increment(2);
        foreach (var x in services)
        {
            x.Count.Is(1);
            x.Sum.Is(2);
        }

        (await radio.Send<IAsyncService>().Value()).SequenceEqual([1, 2, 3, 4, 5,]).IsTrue();

        foreach (var x in links)
        {
            x.Dispose();
        }
    }

    [Fact]
    public async Task AllReceiversRunConcurrently()
    {// Every receiver must be invoked before the aggregated task is awaited.
        var radio = new RadioClass();
        var services = Enumerable.Range(1, 4).Select(x => new AsyncService(x, AsyncService.Behavior.Gate)).ToArray();
        var links = services.Select(x => radio.Open<IAsyncService>(x)!).ToArray();

        var task = radio.Send<IAsyncService>().Value();
        task.IsCompleted.IsFalse();

        foreach (var x in services)
        {
            x.ReleaseGate();
        }

        (await task).SequenceEqual([1, 2, 3, 4,]).IsTrue();

        foreach (var x in links)
        {
            x.Dispose();
        }
    }

    [Fact]
    public async Task EmptyResultsAreSkipped()
    {
        var radio = new RadioClass();

        using (radio.Open<IAsyncService>(new AsyncService(0)))
        using (radio.Open<IAsyncService>(new AsyncService(1)))
        using (radio.Open<IAsyncService>(new AsyncService(0)))
        using (radio.Open<IAsyncService>(new AsyncService(2)))
        {
            var result = await radio.Send<IAsyncService>().Value();
            result.Count.Is(2);
            result.SequenceEqual([1, 2,]).IsTrue();
        }
    }

    [Fact]
    public async Task AllResultsAreEmpty()
    {
        var radio = new RadioClass();

        using (radio.Open<IAsyncService>(new AsyncService(0)))
        using (radio.Open<IAsyncService>(new AsyncService(0)))
        {
            (await radio.Send<IAsyncService>().Value()).IsEmpty.IsTrue();
        }
    }

    [Fact]
    public async Task SingleValidResultAmongMany()
    {
        var radio = new RadioClass();

        using (radio.Open<IAsyncService>(new AsyncService(0)))
        using (radio.Open<IAsyncService>(new AsyncService(0)))
        using (radio.Open<IAsyncService>(new AsyncService(6)))
        {
            var result = await radio.Send<IAsyncService>().Value();
            result.Count.Is(1);
            result.SequenceEqual([6,]).IsTrue();
        }
    }

    [Fact]
    public async Task NullableResult()
    {
        var radio = new RadioClass();

        using (radio.Open<IAsyncService>(new AsyncService(-1))) // Returns a single null.
        using (radio.Open<IAsyncService>(new AsyncService(0))) // Returns an empty result.
        using (radio.Open<IAsyncService>(new AsyncService(7)))
        {
            var result = await radio.Send<IAsyncService>().Text();
            result.Count.Is(2);
            result.SequenceEqual([null, "7",]).IsTrue();
        }
    }

    [Fact]
    public async Task SynchronousExceptionIsCapturedInTheTask()
    {// The broker methods are not 'async', so a synchronous exception must not escape before the task is awaited.
        var radio = new RadioClass();

        using (radio.Open<IAsyncService>(new AsyncService(1, AsyncService.Behavior.ThrowSynchronously)))
        {
            var task = radio.Send<IAsyncService>().Increment(1); // Must not throw here.
            task.IsFaulted.IsTrue();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);

            var task2 = radio.Send<IAsyncService>().Value();
            task2.IsFaulted.IsTrue();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await task2);
        }
    }

    [Fact]
    public async Task AsynchronousExceptionIsPropagated()
    {
        var radio = new RadioClass();

        using (radio.Open<IAsyncService>(new AsyncService(1, AsyncService.Behavior.ThrowAsynchronously)))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await radio.Send<IAsyncService>().Increment(1));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await radio.Send<IAsyncService>().Value());
        }
    }

    [Fact]
    public async Task ExceptionAmongMultipleReceivers()
    {
        var radio = new RadioClass();
        var service1 = new AsyncService(1);
        var service2 = new AsyncService(2, AsyncService.Behavior.ThrowAsynchronously);
        var service3 = new AsyncService(3);

        using (radio.Open<IAsyncService>(service1))
        using (radio.Open<IAsyncService>(service2))
        using (radio.Open<IAsyncService>(service3))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await radio.Send<IAsyncService>().Increment(1));

            // The other receivers must still have been invoked.
            service1.Count.Is(1);
            service3.Count.Is(1);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await radio.Send<IAsyncService>().Value());
        }
    }

    [Fact]
    public async Task SynchronousExceptionAmongMultipleReceivers()
    {// The tasks already started are abandoned, but the exception must be observed via the returned task.
        var radio = new RadioClass();

        using (radio.Open<IAsyncService>(new AsyncService(1)))
        using (radio.Open<IAsyncService>(new AsyncService(2, AsyncService.Behavior.ThrowSynchronously)))
        {
            var task = radio.Send<IAsyncService>().Increment(1); // Must not throw here.
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
        }
    }

    [Fact]
    public async Task ManyReceivers()
    {
        var radio = new RadioClass();
        var links = Enumerable.Range(1, 50).Select(x => radio.Open<IAsyncService>(new AsyncService(x))!).ToArray();

        (await radio.Send<IAsyncService>().Value()).SequenceEqual(Enumerable.Range(1, 50)).IsTrue();

        for (var i = 0; i < 50; i += 2)
        {
            links[i].Dispose();
        }

        (await radio.Send<IAsyncService>().Value()).SequenceEqual(Enumerable.Range(1, 50).Where(x => (x % 2) == 0)).IsTrue();

        foreach (var x in links)
        {
            x.Dispose();
        }
    }

    [Fact]
    public async Task DeadWeakReferencesAreSkipped()
    {
        var radio = new RadioClass();
        var service = new AsyncService(4);

        void OpenTemporaryInstances()
        {
            for (var i = 0; i < 8; i++)
            {
                radio.Open<IAsyncService>(new AsyncService(9), true);
            }
        }

        OpenTemporaryInstances();
        using (radio.Open<IAsyncService>(service, true))
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            await radio.Send<IAsyncService>().Increment(1);
            service.Count.Is(1);
            (await radio.Send<IAsyncService>().Value()).SequenceEqual([4,]).IsTrue();
        }

        GC.KeepAlive(service);
    }
}
