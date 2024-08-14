// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using Xunit;

namespace XUnitTest;

public class StaticTest
{ // I prefer static Radio class. but because it's a static class, it causes testing problems.
   // So this is the only test code for the static class.

    [Fact]
    public void Static_TwoWay()
    {
        using (Radio.Open<ITestService>(new TestService()))
        {
            Radio.GetChannel<ITestService>().Count.Is(1);
            Radio.Send<ITestService>().Double(1).SequenceEqual([2]).IsTrue();
            Radio.Send<ITestService>().Double(2).SequenceEqual([4]).IsTrue();

            using (Radio.Open<ITestService>(new TestService()))
            {
                Radio.GetChannel<ITestService>().Count.Is(2);
                Radio.Send<ITestService>().Double(1).SequenceEqual([2, 2]).IsTrue();
                Radio.Send<ITestService>().Double(2).SequenceEqual([4, 4]).IsTrue();
            }

            Radio.Send<ITestService>().Double(3).SequenceEqual([6]).IsTrue();
        }

        Radio.GetChannel<ITestService>().Count.Is(0);
        Radio.Send<ITestService>().Double(4).SequenceEqual([]).IsTrue();
    }

    [Fact]
    public void Static_TwoWayKey()
    {
        using (Radio.Open<ITestService, int>(new TestService(), 1))
        {
            Radio.GetChannel<ITestService>().Count.Is(0);
            Radio.Send<ITestService>().Double(1).SequenceEqual([]).IsTrue();
            Radio.Send<ITestService, int>(0).Double(2).SequenceEqual([]).IsTrue();
            Radio.Send<ITestService, int>(1).Double(2).SequenceEqual([4]).IsTrue();
            if (Radio.TryGetChannel<ITestService, int>(1, out var channel))
            {
                channel.Count.Is(1);
            }

            using (Radio.Open<ITestService, int>(new TestService(), 2))
            {
                Radio.Send<ITestService>().Double(1).SequenceEqual([]).IsTrue();
                Radio.Send<ITestService, int>(0).Double(2).SequenceEqual([]).IsTrue();
                Radio.Send<ITestService, int>(1).Double(2).SequenceEqual([4]).IsTrue();
                Radio.Send<ITestService, int>(2).Double(3).SequenceEqual([6]).IsTrue();
            }
        }

        Radio.Send<ITestService>().Double(4).SequenceEqual([]).IsTrue();
    }
}
