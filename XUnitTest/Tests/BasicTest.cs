// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using Xunit;

namespace XUnitTest;

public class BasicTest
{
    [Fact]
    public void Class_TwoWay()
    {
        var radio = new RadioClass();
        using (radio.Open((ITestService)new TestService()))
        {
            radio.Send<ITestService>().Double(1).SequenceEqual([2, ]).IsTrue();
            radio.Send<ITestService>().Double(2).SequenceEqual([4,]).IsTrue();

            using (radio.Open((ITestService)new TestService()))
            {
                radio.Send<ITestService>().Double(1).ToArray().SequenceEqual([2, 2,]).IsTrue();
                radio.Send<ITestService>().Double(2).ToArray().SequenceEqual([4, 4, ]).IsTrue();
            }

            radio.Send<ITestService>().Double(3).SequenceEqual([6, ]).IsTrue();
        }

        radio.Send<ITestService>().Double(4).SequenceEqual([]).IsTrue();
    }
}
