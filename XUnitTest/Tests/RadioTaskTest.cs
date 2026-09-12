// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
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
        (await RadioTask.AggregateAsync<int>(Task.FromResult(Array.Empty<RadioResult<int>>()))).IsEmpty.IsTrue();
        (await RadioTask.AggregateAsync<int>(Task.FromResult<RadioResult<int>[]>([default, default,]))).IsEmpty.IsTrue();
    }

    [Fact]
    public async Task AggregateSingle()
    {
        var result = await RadioTask.AggregateAsync<int>(Task.FromResult<RadioResult<int>[]>([new(5),]));
        result.Count.Is(1);
        result.SequenceEqual([5,]).IsTrue();

        // A single valid result among empty ones.
        result = await RadioTask.AggregateAsync<int>(Task.FromResult<RadioResult<int>[]>([default, new(6), default,]));
        result.Count.Is(1);
        result.SequenceEqual([6,]).IsTrue();
    }

    [Fact]
    public async Task AggregateMultiple()
    {
        var result = await RadioTask.AggregateAsync<int>(Task.FromResult<RadioResult<int>[]>([new(1), new(2), new(3),]));
        result.Count.Is(3);
        result.SequenceEqual([1, 2, 3,]).IsTrue();

        // The empty results are skipped and the aggregated array is trimmed.
        result = await RadioTask.AggregateAsync<int>(Task.FromResult<RadioResult<int>[]>([default, new(1), default, new(2), default,]));
        result.Count.Is(2);
        result.SequenceEqual([1, 2,]).IsTrue();
    }

    [Fact]
    public async Task AggregateTakesTheFirstResultOfEach()
    {
        var result = await RadioTask.AggregateAsync<int>(Task.FromResult<RadioResult<int>[]>([new([1, 2,]), new([3, 4,]),]));
        result.SequenceEqual([1, 3,]).IsTrue();
    }

    [Fact]
    public async Task AggregateNullValues()
    {
        var result = await RadioTask.AggregateAsync<string?>(Task.FromResult<RadioResult<string?>[]>(
            [RadioResult<string?>.FromValue(null), default, RadioResult<string?>.FromValue("a"),]));

        // A null value is a valid result.
        result.Count.Is(2);
        result.SequenceEqual([null, "a",]).IsTrue();
    }

    [Fact]
    public async Task AggregateFaultedTask()
    {
        var faulted = Task.FromException<RadioResult<int>[]>(new InvalidOperationException());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await RadioTask.AggregateAsync<int>(faulted));
    }
}
