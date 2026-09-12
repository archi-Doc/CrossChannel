// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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
    private Task<RadioResult<int>[]> sparseResults = default!;

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
        this.sparseResults = Task.FromResult<RadioResult<int>[]>([default, new(1), default, new(2), default, default, default, default]);
    }

    [Benchmark]
    public void Notify() => this.broker.Notify();

    [Benchmark]
    public Task CompletedTasks() => this.broker.Complete();

    [Benchmark]
    public RadioResult<int> Result() => this.broker.Value();

    [Benchmark]
    public Task<RadioResult<int>> SparseAggregate() => RadioTask.AggregateAsync(this.sparseResults);

    public static void CheckAllocations()
    {
        foreach (var receivers in new[] { 0, 1, 8 })
        {
            var benchmark = new AllocationBenchmark { Receivers = receivers };
            benchmark.Setup();
            Measure("Notify", receivers, benchmark.Notify);
            Measure("CompletedTasks", receivers, () => benchmark.CompletedTasks().GetAwaiter().GetResult());
            Measure("Result", receivers, () => _ = benchmark.Result());
            if (receivers == 8)
            {
                Measure("SparseAggregate", receivers, () => benchmark.SparseAggregate().GetAwaiter().GetResult());
            }
        }

        static void Measure(string name, int receivers, Action action)
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
            Console.WriteLine($"{name}, receivers={receivers}, bytes/op={allocated / 100_000d:F2}");
        }
    }
}
