// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using Xunit;

namespace XUnitTest;

public class CleanupTest
{
    [Fact]
    public void WeakReference()
    {
        var radio = new RadioClass();

        void CreateChannel()
        {
            radio.Open<ITestService>(new TestService(), true);
        }

        radio.Send<ITestService>().Double(1).Count.Is(0);

        for (var i = 0; i < RadioConstants.CleanupListThreshold; i++)
        {
            CreateChannel();
        }

        GC.Collect(); // Empty list
        radio.Send<ITestService>().Double(1).Count.Is(0);

        var objects = Enumerable.Repeat(new TestService(), RadioConstants.CleanupListThreshold).ToArray();
        var number = 0;

        for (var i = 0; i < RadioConstants.CleanupListThreshold; i++)
        {
            if (i % 3 == 0)
            {
                radio.Open<ITestService>(objects[i], true);
                number++;
            }
            else
            {
                CreateChannel();
            }
        }

        GC.Collect();
        radio.Send<ITestService>().Double(1).Count.Is(number);

        radio.Open<ITestService>(new TestService(), true);
        radio.Send<ITestService>().Double(1).Count.Is(number + 1);

        GC.KeepAlive(objects);
    }
}
