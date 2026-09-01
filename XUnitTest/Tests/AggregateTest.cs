// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using CrossChannel;
using Xunit;

namespace XUnitTest;

[RadioService]
public interface IAggregateService : IRadioService
{
    RadioResult<int> Get();

    Task<RadioResult<int>> GetAsync();
}

public class AggregateService : IAggregateService
{
    private readonly int value; // 0: returns an empty result.

    public AggregateService(int value)
    {
        this.value = value;
    }

    RadioResult<int> IAggregateService.Get()
        => this.value == 0 ? default : new(this.value);

    async Task<RadioResult<int>> IAggregateService.GetAsync()
    {
        await Task.Yield();
        return this.value == 0 ? default : new(this.value);
    }
}

public class AggregateTest
{
    [Fact]
    public void Sync()
    {
        var radio = new RadioClass();
        radio.Send<IAggregateService>().Get().IsEmpty.IsTrue();

        using (radio.Open<IAggregateService>(new AggregateService(1)))
        using (radio.Open<IAggregateService>(new AggregateService(0)))
        using (radio.Open<IAggregateService>(new AggregateService(2)))
        {
            // An empty result is not aggregated.
            radio.Send<IAggregateService>().Get().SequenceEqual([1, 2,]).IsTrue();
        }
    }

    [Fact]
    public async Task Async()
    {
        var radio = new RadioClass();
        (await radio.Send<IAggregateService>().GetAsync()).IsEmpty.IsTrue();

        using (radio.Open<IAggregateService>(new AggregateService(1)))
        {
            (await radio.Send<IAggregateService>().GetAsync()).SequenceEqual([1,]).IsTrue();

            using (radio.Open<IAggregateService>(new AggregateService(0)))
            using (radio.Open<IAggregateService>(new AggregateService(2)))
            {
                // An empty result must be skipped, in the same way as the synchronous version.
                (await radio.Send<IAggregateService>().GetAsync()).SequenceEqual([1, 2,]).IsTrue();
            }
        }
    }
}
