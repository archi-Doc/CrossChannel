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
        var radio = new RadioClass();

        void CreateChannel()
        {
            radio.Open<ITestService>(new TestService(), true);
        }

        radio.Send<ITestService>().Double(1).Count.Is(0);

        for (var i = 0; i < RadioConstants.ChannelTrimThreshold; i++)
        {
            CreateChannel();
        }

        radio.GetChannel<ITestService>().Count.Is(RadioConstants.ChannelTrimThreshold);
        GC.Collect(); // Empty list
        radio.Send<ITestService>().Double(1).Count.Is(0);

        var objects = Enumerable.Repeat(new TestService(), RadioConstants.ChannelTrimThreshold).ToArray();
        var number = 0;

        for (var i = 0; i < RadioConstants.ChannelTrimThreshold; i++)
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
