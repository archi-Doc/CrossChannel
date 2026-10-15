// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CrossChannel;
using Xunit;

namespace XUnitTest;

public class RadioTaskTest
{
    [Fact]
    public async Task GetEmptyResultTask()
    {
        var task = RadioTask.GetEmptyResultTask<int>();
        task.IsCompletedSuccessfully.IsTrue();
        (await task).IsEmpty.IsTrue();

        // The task is cached per result type.
        ReferenceEquals(task, RadioTask.GetEmptyResultTask<int>()).IsTrue();
        ReferenceEquals(task, RadioTask.GetEmptyResultTask<long>()).IsFalse();

        (await RadioTask.GetEmptyResultTask<string>()).IsEmpty.IsTrue();
        (await RadioTask.GetEmptyResultTask<string?>()).IsEmpty.IsTrue();
    }

    [Fact]
    public async Task AggregateNothing()
    {
        (await Aggregate<int>()).IsEmpty.IsTrue();
        (await AggregateCompleted<int>(default, default)).IsEmpty.IsTrue();

        // Empty results of completed tasks are aggregated synchronously into the cached empty task.
        ReferenceEquals(Aggregate<int>(), RadioTask.GetEmptyResultTask<int>()).IsTrue();
        ReferenceEquals(AggregateCompleted<int>(default, default), RadioTask.GetEmptyResultTask<int>()).IsTrue();
    }

    [Fact]
    public async Task AggregateSingle()
    {
        var task = AggregateCompleted(new RadioResult<int>(5));
        task.IsCompletedSuccessfully.IsTrue();
        var result = await task;
        result.Count.Is(1);
        result.SequenceEqual([5,]).IsTrue();

        // A single valid result among empty ones.
        result = await AggregateCompleted<int>(default, new(6), default);
        result.Count.Is(1);
        result.SequenceEqual([6,]).IsTrue();
    }

    [Fact]
    public async Task AggregateMultiple()
    {
        // Completed tasks are aggregated synchronously, in receiver order.
        var task = AggregateCompleted<int>(new(1), new(2), new(3));
        task.IsCompletedSuccessfully.IsTrue();
        var result = await task;
        result.Count.Is(3);
        result.SequenceEqual([1, 2, 3,]).IsTrue();

        // The empty results are skipped and the aggregated array is trimmed.
        result = await AggregateCompleted<int>(default, new(1), default, new(2), default);
        result.Count.Is(2);
        result.SequenceEqual([1, 2,]).IsTrue();
    }

    [Fact]
    public async Task AggregateTakesTheFirstResultOfEach()
    {
        var result = await AggregateCompleted<int>(new([1, 2,]), new([3, 4,]));
        result.SequenceEqual([1, 3,]).IsTrue();
    }

    [Fact]
    public async Task AggregateNullValues()
    {
        var result = await AggregateCompleted<string?>(RadioResult<string?>.FromValue(null), default, RadioResult<string?>.FromValue("a"));

        // A null value is a valid result.
        result.Count.Is(2);
        result.SequenceEqual([null, "a",]).IsTrue();
    }

    [Fact]
    public async Task AggregateOnlyTheFirstCountTasks()
    {
        // The broker passes its own array, which may be larger than the number of receivers.
        var tasks = new Task<RadioResult<int>>[4];
        tasks[0] = Task.FromResult(new RadioResult<int>(1));
        tasks[1] = Task.FromResult(new RadioResult<int>(2));
        (await RadioTask.AggregateAsync(tasks, 2)).SequenceEqual([1, 2,]).IsTrue();

        var pending = new TaskCompletionSource<RadioResult<int>>();
        tasks[1] = pending.Task;
        var task = RadioTask.AggregateAsync(tasks, 2);
        task.IsCompleted.IsFalse();
        pending.SetResult(new(3));
        (await task).SequenceEqual([1, 3,]).IsTrue();
    }

    [Fact]
    public async Task AggregatePendingTasks()
    {
        var first = new TaskCompletionSource<RadioResult<int>>();
        var third = new TaskCompletionSource<RadioResult<int>>();
        var task = Aggregate(first.Task, Task.FromResult(new RadioResult<int>(2)), third.Task, RadioTask.GetEmptyResultTask<int>());
        task.IsCompleted.IsFalse();

        // The results keep the receiver order, not the completion order.
        third.SetResult(new(3));
        task.IsCompleted.IsFalse();
        first.SetResult(new(1));
        (await task).SequenceEqual([1, 2, 3,]).IsTrue();

        // Pending tasks which complete without a result.
        first = new TaskCompletionSource<RadioResult<int>>();
        task = Aggregate(first.Task, RadioTask.GetEmptyResultTask<int>());
        first.SetResult(default);
        (await task).IsEmpty.IsTrue();
    }

    [Fact]
    public async Task AggregateFaultedTask()
    {
        // A synchronously faulted task among completed ones.
        var task = Aggregate(Task.FromResult(new RadioResult<int>(1)), Task.FromException<RadioResult<int>>(new InvalidOperationException()));
        task.IsFaulted.IsTrue();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
    }

    [Fact]
    public async Task AggregateKeepsEveryExceptionInReceiverOrder()
    {
        var a = new InvalidOperationException("a");
        var b = new ArgumentException("b");
        var pending = new TaskCompletionSource<RadioResult<int>>();
        var task = Aggregate(Task.FromException<RadioResult<int>>(a), Task.FromResult(new RadioResult<int>(1)), pending.Task);
        task.IsCompleted.IsFalse();

        pending.SetException(b);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
        ReferenceEquals(exception, a).IsTrue(); // 'await' throws the exception of the first faulted receiver.
        task.Exception!.InnerExceptions.SequenceEqual([a, b,]).IsTrue();

        // The second receiver faults first, but the exceptions keep the receiver order.
        var first = new TaskCompletionSource<RadioResult<int>>();
        var second = new TaskCompletionSource<RadioResult<int>>();
        task = Aggregate(first.Task, second.Task);
        second.SetException(b);
        first.SetException(a);
        exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
        ReferenceEquals(exception, a).IsTrue();
        task.Exception!.InnerExceptions.SequenceEqual([a, b,]).IsTrue();
    }

    [Fact]
    public async Task AggregateCanceledTask()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var task = Aggregate(Task.FromResult(new RadioResult<int>(1)), Task.FromCanceled<RadioResult<int>>(cts.Token));
        task.IsCanceled.IsTrue();
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
        exception.CancellationToken.Is(cts.Token);

        // A pending task which is canceled later.
        var pending = new TaskCompletionSource<RadioResult<int>>();
        task = Aggregate(Task.FromResult(new RadioResult<int>(1)), pending.Task);
        pending.SetCanceled(cts.Token);
        exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
        task.IsCanceled.IsTrue();
        exception.CancellationToken.Is(cts.Token);

        // A fault takes precedence over a cancellation.
        task = Aggregate(Task.FromCanceled<RadioResult<int>>(cts.Token), Task.FromException<RadioResult<int>>(new InvalidOperationException()));
        task.IsFaulted.IsTrue();

        // The exception of the canceled receiver is passed on as is.
        var gate = new TaskCompletionSource();
        var canceled = new CustomCanceledException(cts.Token);
        task = Aggregate(CancelAfter(gate.Task, canceled), Task.FromResult(new RadioResult<int>(1)));
        task.IsCompleted.IsFalse();
        gate.SetResult();
        exception = await Assert.ThrowsAsync<CustomCanceledException>(async () => await task);
        ReferenceEquals(exception, canceled).IsTrue();
        task.IsCanceled.IsTrue();

        static async Task<RadioResult<int>> CancelAfter(Task gate, OperationCanceledException exception)
        {// An async method which throws an OperationCanceledException completes as canceled, keeping the exception.
            await gate;
            throw exception;
        }
    }

    [Fact]
    public async Task HandledFaultsAreNotReportedAsUnobserved()
    {// The exceptions of an awaited aggregation are observed, so TaskScheduler.UnobservedTaskException is not raised.
        const string message = "HandledFaultsAreNotReportedAsUnobserved";
        var unobserved = 0;
        EventHandler<UnobservedTaskExceptionEventArgs> handler = (_, e) =>
        {
            if (e.Exception.Flatten().InnerExceptions.Any(x => x.Message == message))
            {
                Interlocked.Increment(ref unobserved);
            }
        };

        TaskScheduler.UnobservedTaskException += handler;
        try
        {
            for (var i = 0; i < 3; i++)
            {
                await AwaitFaultedAggregation(message);
            }

            for (var i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Volatile.Read(ref unobserved).Is(0);
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= handler;
        }
    }

    [Fact]
    public void AggregateInvalidArguments()
    {
        // A null task fails like Task.WhenAll.
        Assert.Throws<ArgumentException>(() => { _ = Aggregate(Task.FromResult(new RadioResult<int>(1)), null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = RadioTask.AggregateAsync<int>(null!, 0); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = RadioTask.AggregateAsync(new Task<RadioResult<int>>[1], 2); });
    }

    [Fact]
    public async Task AggregateTheTaskOfTheResults()
    {// The overload called by the brokers generated by CrossChannel 3.1.0 (kept for binary compatibility).
        (await RadioTask.AggregateAsync(Task.FromResult<RadioResult<int>[]>([]))).IsEmpty.IsTrue();
        (await RadioTask.AggregateAsync(Task.FromResult<RadioResult<int>[]>([default, new(5), default,]))).SequenceEqual([5,]).IsTrue();
        (await RadioTask.AggregateAsync(Task.FromResult<RadioResult<int>[]>([new([1, 2,]), default, new(3),]))).SequenceEqual([1, 3,]).IsTrue();

        var source = new TaskCompletionSource<RadioResult<int>[]>();
        var task = RadioTask.AggregateAsync(source.Task);
        task.IsCompleted.IsFalse();
        source.SetResult([new(4), new(5),]);
        (await task).SequenceEqual([4, 5,]).IsTrue();

        await Assert.ThrowsAsync<InvalidOperationException>(() => RadioTask.AggregateAsync(Task.FromException<RadioResult<int>[]>(new InvalidOperationException())));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task AwaitFaultedAggregation(string message)
    {
        var first = new TaskCompletionSource<RadioResult<int>>();
        var task = Aggregate(first.Task, Task.FromException<RadioResult<int>>(new InvalidOperationException(message)));
        first.SetException(new InvalidOperationException(message));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
    }

    private static Task<RadioResult<T>> Aggregate<T>(params Task<RadioResult<T>>[] tasks)
        => RadioTask.AggregateAsync(tasks, tasks.Length);

    private static Task<RadioResult<T>> AggregateCompleted<T>(params RadioResult<T>[] results)
        => Aggregate(results.Select(x => Task.FromResult(x)).ToArray());

    private sealed class CustomCanceledException(CancellationToken token)
        : OperationCanceledException("Custom", token)
    {
    }
}
