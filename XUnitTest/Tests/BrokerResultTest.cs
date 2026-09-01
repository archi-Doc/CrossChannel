// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using Xunit;

namespace XUnitTest;

[RadioService]
public interface IResultService : IRadioService
{
    RadioResult<int> Value();

    RadioResult<string?> Text();

    RadioResult<int> Multiple();

    RadioResult<int> Throw();
}

public class ResultService : IResultService
{
    private readonly int value; // 0: returns an empty result.

    public ResultService(int value)
    {
        this.value = value;
    }

    RadioResult<int> IResultService.Value()
        => this.value == 0 ? default : new(this.value);

    RadioResult<string?> IResultService.Text()
        => this.value == 0 ? default : RadioResult<string?>.Single(this.value == -1 ? null : this.value.ToString());

    RadioResult<int> IResultService.Multiple()
        => new([this.value, this.value * 10, this.value * 100,]);

    RadioResult<int> IResultService.Throw()
        => throw new InvalidOperationException();
}

public class BrokerResultTest
{
    [Fact]
    public void NoReceiver()
    {
        var radio = new RadioClass();
        var result = radio.Send<IResultService>().Value();
        result.IsEmpty.IsTrue();
        result.Count.Is(0);
        result.SequenceEqual([]).IsTrue();
    }

    [Fact]
    public void SingleReceiver()
    {
        var radio = new RadioClass();

        using (radio.Open<IResultService>(new ResultService(3)))
        {
            var result = radio.Send<IResultService>().Value();
            result.Count.Is(1);
            result.TryGetSingleResult(out var r).IsTrue();
            r.Is(3);
            result.SequenceEqual([3,]).IsTrue();
        }
    }

    [Fact]
    public void SingleReceiverWithEmptyResult()
    {
        var radio = new RadioClass();

        using (radio.Open<IResultService>(new ResultService(0)))
        {
            radio.Send<IResultService>().Value().IsEmpty.IsTrue();
        }
    }

    [Fact]
    public void MultipleReceivers()
    {
        var radio = new RadioClass();

        using (radio.Open<IResultService>(new ResultService(1)))
        using (radio.Open<IResultService>(new ResultService(2)))
        using (radio.Open<IResultService>(new ResultService(3)))
        {
            // The results are aggregated in the order the links were opened.
            radio.Send<IResultService>().Value().SequenceEqual([1, 2, 3,]).IsTrue();
        }
    }

    [Fact]
    public void EmptyResultsAreSkipped()
    {
        var radio = new RadioClass();

        using (radio.Open<IResultService>(new ResultService(0)))
        using (radio.Open<IResultService>(new ResultService(1)))
        using (radio.Open<IResultService>(new ResultService(0)))
        using (radio.Open<IResultService>(new ResultService(2)))
        using (radio.Open<IResultService>(new ResultService(0)))
        {
            // 5 links, but only 2 results (the aggregated array must be trimmed).
            var result = radio.Send<IResultService>().Value();
            result.Count.Is(2);
            result.SequenceEqual([1, 2,]).IsTrue();
        }
    }

    [Fact]
    public void AllResultsAreEmpty()
    {
        var radio = new RadioClass();

        using (radio.Open<IResultService>(new ResultService(0)))
        using (radio.Open<IResultService>(new ResultService(0)))
        {
            radio.Send<IResultService>().Value().IsEmpty.IsTrue();
        }
    }

    [Fact]
    public void SingleValidResultAmongMany()
    {// The aggregation must collapse into a single result (no array).
        var radio = new RadioClass();

        using (radio.Open<IResultService>(new ResultService(0)))
        using (radio.Open<IResultService>(new ResultService(0)))
        using (radio.Open<IResultService>(new ResultService(7)))
        using (radio.Open<IResultService>(new ResultService(0)))
        {
            var result = radio.Send<IResultService>().Value();
            result.Count.Is(1);
            result.SequenceEqual([7,]).IsTrue();
        }
    }

    [Fact]
    public void ManyReceivers()
    {
        var radio = new RadioClass();
        var links = Enumerable.Range(1, 100).Select(x => radio.Open<IResultService>(new ResultService(x))!).ToArray();

        radio.GetChannel<IResultService>().Count.Is(100);
        radio.Send<IResultService>().Value().SequenceEqual(Enumerable.Range(1, 100)).IsTrue();

        // Close half of them; the remaining ones must still be aggregated.
        for (var i = 0; i < 100; i += 2)
        {
            links[i].Dispose();
        }

        radio.Send<IResultService>().Value().SequenceEqual(Enumerable.Range(1, 100).Where(x => (x % 2) == 0)).IsTrue();

        foreach (var x in links)
        {
            x.Dispose();
        }
    }

    [Fact]
    public void ReferenceTypeAndNull()
    {
        var radio = new RadioClass();

        using (radio.Open<IResultService>(new ResultService(-1))) // Returns a single null.
        using (radio.Open<IResultService>(new ResultService(0))) // Returns an empty result.
        using (radio.Open<IResultService>(new ResultService(5)))
        {
            var result = radio.Send<IResultService>().Text();

            // A null value is a valid result and must not be skipped.
            result.Count.Is(2);
            result.SequenceEqual([null, "5",]).IsTrue();
        }
    }

    [Fact]
    public void OnlyTheFirstResultOfEachReceiverIsAggregated()
    {// The receiver-side result is expected to be singular.
        var radio = new RadioClass();

        using (radio.Open<IResultService>(new ResultService(1)))
        using (radio.Open<IResultService>(new ResultService(2)))
        {
            radio.Send<IResultService>().Multiple().SequenceEqual([1, 2,]).IsTrue();
        }
    }

    [Fact]
    public void ExceptionIsPropagated()
    {
        var radio = new RadioClass();

        using (radio.Open<IResultService>(new ResultService(1)))
        {
            Assert.Throws<InvalidOperationException>(() => radio.Send<IResultService>().Throw());
        }
    }

    [Fact]
    public void DeadWeakReferencesAreSkipped()
    {
        var radio = new RadioClass();
        var service = new ResultService(4);

        void OpenTemporaryInstances()
        {
            for (var i = 0; i < 8; i++)
            {
                radio.Open<IResultService>(new ResultService(9), true);
            }
        }

        OpenTemporaryInstances();
        using (radio.Open<IResultService>(service, true))
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            radio.Send<IResultService>().Value().SequenceEqual([4,]).IsTrue();
        }

        GC.KeepAlive(service);
    }
}
