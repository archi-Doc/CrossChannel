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
        Completed, // Returns a task which has already completed.
        Faulted, // Returns a task which has already faulted.
    }

    public int Count { get; private set; }

    public int Sum { get; private set; }

    public int IncrementCalls { get; private set; }

    public int ValueCalls { get; private set; }

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
        this.IncrementCalls++;
        if (this.behavior == Behavior.ThrowSynchronously)
        {
            throw new InvalidOperationException(this.value.ToString());
        }
        else if (this.behavior == Behavior.Gate)
        {
            return this.gate.Task;
        }
        else if (this.behavior == Behavior.Completed)
        {
            this.Count++;
            this.Sum += x;
            return Task.CompletedTask;
        }
        else if (this.behavior == Behavior.Faulted)
        {
            return Task.FromException(new InvalidOperationException(this.value.ToString()));
        }

        return this.IncrementAsync(x);
    }

    Task<RadioResult<int>> IAsyncService.Value()
    {
        this.ValueCalls++;
        if (this.behavior == Behavior.ThrowSynchronously)
        {
            throw new InvalidOperationException(this.value.ToString());
        }
        else if (this.behavior == Behavior.Gate)
        {
            return this.valueGate.Task;
        }
        else if (this.behavior == Behavior.Completed)
        {
            return this.value == 0 ? RadioTask.GetEmptyResultTask<int>() : Task.FromResult(new RadioResult<int>(this.value));
        }
        else if (this.behavior == Behavior.Faulted)
        {
            return Task.FromException<RadioResult<int>>(new InvalidOperationException(this.value.ToString()));
        }

        return this.ValueAsync();
    }

    Task<RadioResult<string?>> IAsyncService.Text()
    {
        if (this.behavior == Behavior.ThrowSynchronously)
        {
            throw new InvalidOperationException(this.value.ToString());
        }

        return this.TextAsync();
    }

    private async Task IncrementAsync(int x)
    {
        await Task.Yield();
        if (this.behavior == Behavior.ThrowAsynchronously)
        {
            throw new InvalidOperationException(this.value.ToString());
        }

        this.Count++;
        this.Sum += x;
    }

    private async Task<RadioResult<int>> ValueAsync()
    {
        await Task.Yield();
        if (this.behavior == Behavior.ThrowAsynchronously)
        {
            throw new InvalidOperationException(this.value.ToString());
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

        return RadioResult<string?>.FromValue(this.value == -1 ? null : this.value.ToString());
    }
}

public class BrokerAsyncTest
{
    [Fact]
    public async Task NoReceiver()
    {
        var radio = new LocalRadio();

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
        var radio = new LocalRadio();
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
        var radio = new LocalRadio();
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
        var radio = new LocalRadio();
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
        var radio = new LocalRadio();
        var services = Enumerable.Range(1, 4).Select(x => new AsyncService(x, AsyncService.Behavior.Gate)).ToArray();
        var links = services.Select(x => radio.Open<IAsyncService>(x)!).ToArray();

        var task = radio.Send<IAsyncService>().Value();
        task.IsCompleted.IsFalse();
        services.All(x => x.ValueCalls == 1).IsTrue(); // Every receiver was invoked before any of them completed.

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
    public async Task CompletedEmptyResultsReturnTheCachedEmptyTask()
    {// When every receiver has already completed, the results are aggregated synchronously.
        var radio = new LocalRadio();

        using (radio.Open<IAsyncService>(new AsyncService(0, AsyncService.Behavior.Completed)))
        using (radio.Open<IAsyncService>(new AsyncService(0, AsyncService.Behavior.Completed)))
        using (radio.Open<IAsyncService>(new AsyncService(0, AsyncService.Behavior.Completed)))
        {
            var task = radio.Send<IAsyncService>().Value();
            ReferenceEquals(task, RadioTask.GetEmptyResultTask<int>()).IsTrue();
            (await task).IsEmpty.IsTrue();

            var task2 = radio.Send<IAsyncService>().Increment(1);
            task2.IsCompletedSuccessfully.IsTrue();
            await task2;
        }
    }

    [Fact]
    public async Task CompletedSingleResultAmongMany()
    {
        var radio = new LocalRadio();

        using (radio.Open<IAsyncService>(new AsyncService(0, AsyncService.Behavior.Completed)))
        using (radio.Open<IAsyncService>(new AsyncService(5, AsyncService.Behavior.Completed)))
        using (radio.Open<IAsyncService>(new AsyncService(0, AsyncService.Behavior.Completed)))
        {
            var task = radio.Send<IAsyncService>().Value();
            task.IsCompletedSuccessfully.IsTrue();
            var result = await task;
            result.Count.Is(1);
            result.SequenceEqual([5,]).IsTrue();
        }
    }

    [Fact]
    public async Task CompletedResultsKeepTheReceiverOrder()
    {
        var radio = new LocalRadio();

        using (radio.Open<IAsyncService>(new AsyncService(1, AsyncService.Behavior.Completed)))
        using (radio.Open<IAsyncService>(new AsyncService(0, AsyncService.Behavior.Completed)))
        using (radio.Open<IAsyncService>(new AsyncService(2, AsyncService.Behavior.Completed)))
        using (radio.Open<IAsyncService>(new AsyncService(3, AsyncService.Behavior.Completed)))
        {
            var task = radio.Send<IAsyncService>().Value();
            task.IsCompletedSuccessfully.IsTrue();
            (await task).SequenceEqual([1, 2, 3,]).IsTrue();
        }
    }

    [Fact]
    public async Task CompletedResultsMixedWithAPendingOne()
    {
        var radio = new LocalRadio();
        var gate = new AsyncService(2, AsyncService.Behavior.Gate);

        using (radio.Open<IAsyncService>(new AsyncService(1, AsyncService.Behavior.Completed)))
        using (radio.Open<IAsyncService>(gate))
        using (radio.Open<IAsyncService>(new AsyncService(3, AsyncService.Behavior.Completed)))
        {
            var task = radio.Send<IAsyncService>().Value();
            task.IsCompleted.IsFalse(); // Waits for the pending receiver.

            gate.ReleaseGate();
            (await task).SequenceEqual([1, 2, 3,]).IsTrue();
        }
    }

    [Fact]
    public async Task CompletedResultsMixedWithAFaultedOne()
    {
        var radio = new LocalRadio();

        using (radio.Open<IAsyncService>(new AsyncService(1, AsyncService.Behavior.Completed)))
        using (radio.Open<IAsyncService>(new AsyncService(2, AsyncService.Behavior.Faulted)))
        using (radio.Open<IAsyncService>(new AsyncService(3, AsyncService.Behavior.Completed)))
        {
            var task = radio.Send<IAsyncService>().Value();
            task.IsFaulted.IsTrue();
            (await Assert.ThrowsAsync<InvalidOperationException>(async () => await task)).Message.Is("2");

            var task2 = radio.Send<IAsyncService>().Increment(1);
            task2.IsFaulted.IsTrue();
            (await Assert.ThrowsAsync<InvalidOperationException>(async () => await task2)).Message.Is("2");
        }
    }

    [Fact]
    public async Task AllAsynchronousFaultsArePreserved()
    {// Like the Task broker (Task.WhenAll), the Task<RadioResult<T>> broker keeps the exception of every faulted receiver.
        var radio = new LocalRadio();

        using (radio.Open<IAsyncService>(new AsyncService(1, AsyncService.Behavior.ThrowAsynchronously)))
        using (radio.Open<IAsyncService>(new AsyncService(2)))
        using (radio.Open<IAsyncService>(new AsyncService(3, AsyncService.Behavior.ThrowAsynchronously)))
        {
            var task = radio.Send<IAsyncService>().Increment(1);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
            task.Exception!.InnerExceptions.Select(x => x.Message).Order().SequenceEqual(["1", "3",]).IsTrue(); // Task.WhenAll keeps the completion order.

            var task2 = radio.Send<IAsyncService>().Value();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await task2);
            task2.Exception!.InnerExceptions.Select(x => x.Message).SequenceEqual(["1", "3",]).IsTrue(); // In receiver order.
        }
    }

    [Fact]
    public async Task EmptyResultsAreSkipped()
    {
        var radio = new LocalRadio();

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
        var radio = new LocalRadio();

        using (radio.Open<IAsyncService>(new AsyncService(0)))
        using (radio.Open<IAsyncService>(new AsyncService(0)))
        {
            (await radio.Send<IAsyncService>().Value()).IsEmpty.IsTrue();
        }
    }

    [Fact]
    public async Task SingleValidResultAmongMany()
    {
        var radio = new LocalRadio();

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
        var radio = new LocalRadio();

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
        var radio = new LocalRadio();

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
        var radio = new LocalRadio();

        using (radio.Open<IAsyncService>(new AsyncService(1, AsyncService.Behavior.ThrowAsynchronously)))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await radio.Send<IAsyncService>().Increment(1));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await radio.Send<IAsyncService>().Value());
        }
    }

    [Fact]
    public async Task ExceptionAmongMultipleReceivers()
    {
        var radio = new LocalRadio();
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
        var radio = new LocalRadio();
        var service1 = new AsyncService(1);
        var service3 = new AsyncService(3);

        using (radio.Open<IAsyncService>(service1))
        using (radio.Open<IAsyncService>(new AsyncService(2, AsyncService.Behavior.ThrowSynchronously)))
        using (radio.Open<IAsyncService>(service3))
        {
            var task = radio.Send<IAsyncService>().Increment(1); // Must not throw here.
            task.IsFaulted.IsTrue();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);

            var task2 = radio.Send<IAsyncService>().Value(); // Must not throw here, although a task has already been collected.
            task2.IsFaulted.IsTrue();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await task2);

            // The exception stops the broker: the receiver after the thrower is not invoked.
            service1.IncrementCalls.Is(1);
            service1.ValueCalls.Is(1);
            service3.IncrementCalls.Is(0);
            service3.ValueCalls.Is(0);
        }
    }

    [Fact]
    public async Task ManyReceivers()
    {
        var radio = new LocalRadio();
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
        var radio = new LocalRadio();
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
