// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using Xunit;

namespace XUnitTest;

public class TrimTest
{
    [Fact]
    public void WeakReference()
    {
        var radio = new LocalRadio();

        void CreateChannel()
        {
            radio.Open<ITestService>(new TestService(), true);
        }

        radio.Send<ITestService>().Double(1).Count.Is(0);

        for (var i = 0; i < Channel.TrimThreshold; i++)
        {
            CreateChannel();
        }

        radio.GetChannel<ITestService>().Count.Is(Channel.TrimThreshold);
        GC.Collect(); // Empty list
        radio.Send<ITestService>().Double(1).Count.Is(0);

        var objects = Enumerable.Repeat(new TestService(), Channel.TrimThreshold).ToArray();
        var number = 0;

        for (var i = 0; i < Channel.TrimThreshold; i++)
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

        var service = new TestService();
        radio.Open<ITestService>(service, true);
        radio.Send<ITestService>().Double(1).Count.Is(number + 1);

        // Other tests may collect concurrently; keep the weakly linked receivers alive until the sends are done.
        GC.KeepAlive(service);
        GC.KeepAlive(objects);
    }
}
