// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using Xunit;

namespace XUnitTest;

[RadioServiceInterface(SingleLink = true)]
public interface ISingleService : IRadioService
{
    RadioResult<int> Double(int x);
}

public class SingleService : ISingleService
{
    public RadioResult<int> Double(int x)
    {
        return new(x * 2);
    }
}

public class SingleTest
{
    [Fact]
    public void Test1()
    {
        var radio = new RadioClass();

        radio.Send<ISingleService>().Double(1).SequenceEqual([]).IsTrue();

        using var c = radio.Open((ISingleService)new SingleService());
        radio.Send<ISingleService>().Double(2).SequenceEqual([4,]).IsTrue();

        using var c2 = radio.Open((ISingleService)new SingleService());
        radio.Send<ISingleService>().Double(2).SequenceEqual([4,]).IsTrue();
    }
}
