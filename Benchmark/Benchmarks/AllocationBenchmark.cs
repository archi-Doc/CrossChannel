// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using CrossChannel;

namespace Benchmark;

[RadioService]
public interface IAllocationService : IRadioService
{
    void Notify();

    Task Complete();

    RadioResult<int> Value();
}

public class AllocationService : IAllocationService
{
    public void Notify()
    {
    }

    public Task Complete() => Task.CompletedTask;

    public RadioResult<int> Value() => new(1);
}

[MemoryDiagnoser]
[ShortRunJob]
public class AllocationBenchmark
{
    private IAllocationService broker = default!;

    [Params(0, 1, 8)]
    public int Receivers { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var channel = new Channel<IAllocationService>();
        for (var i = 0; i < this.Receivers; i++)
        {
            channel.Open(new AllocationService());
        }

        this.broker = channel.GetBroker();
    }

    [Benchmark]
    public void Notify() => this.broker.Notify();

    [Benchmark]
    public Task CompletedTasks() => this.broker.Complete();

    [Benchmark]
    public RadioResult<int> Result() => this.broker.Value();

    public static void CheckAllocations()
    {
        foreach (var receivers in new[] { 0, 1, 8 })
        {
            var benchmark = new AllocationBenchmark { Receivers = receivers };
            benchmark.Setup();
            Measure($"Notify, receivers={receivers}", benchmark.Notify);
            Measure($"CompletedTasks, receivers={receivers}", () => benchmark.CompletedTasks().GetAwaiter().GetResult());
            Measure($"Result, receivers={receivers}", () => _ = benchmark.Result());
        }

        // SparseAggregate does not depend on the number of receivers, so it is measured once.
        var aggregate = new AggregateAllocationBenchmark();
        Measure("SparseAggregate", () => aggregate.SparseAggregate().GetAwaiter().GetResult());

        static void Measure(string name, Action action)
        {
            for (var i = 0; i < 10_000; i++)
            {
                action();
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100_000; i++)
            {
                action();
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Console.WriteLine($"{name}, bytes/op={allocated / 100_000d:F2}");
        }
    }
}

/// <summary>
/// Measures <see cref="RadioTask.AggregateAsync{T}(Task{RadioResult{T}}[], int)"/> with sparse completed results (2 of 8 receivers respond).<br/>
/// Kept apart from <see cref="AllocationBenchmark"/> because it does not depend on the number of receivers.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class AggregateAllocationBenchmark
{
    private readonly Task<RadioResult<int>>[] sparseResults = [.. new RadioResult<int>[] { default, new(1), default, new(2), default, default, default, default, }.Select(x => Task.FromResult(x))];

    [Benchmark]
    public Task<RadioResult<int>> SparseAggregate() => RadioTask.AggregateAsync(this.sparseResults, this.sparseResults.Length);
}
