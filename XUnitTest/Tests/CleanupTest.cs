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
        var radio = new ObsoleteRadioClass();

        void CreateChannel()
        {
            radio.OpenTwoWay<int, int>(x => x * 2, new object());
        }

        radio.SendTwoWay<int, int>(1).Length.Is(0);

        for (var i = 0; i < RadioConstants.CleanupListThreshold; i++)
        {
            CreateChannel();
        }

        GC.Collect(); // Empty list
        radio.SendTwoWay<int, int>(1).Length.Is(0);

        var objects = Enumerable.Repeat(new object(), RadioConstants.CleanupListThreshold).ToArray();
        var number = 0;

        for (var i = 0; i < RadioConstants.CleanupListThreshold; i++)
        {
            if (i % 3 == 0)
            {
                radio.OpenTwoWay<int, int>(x => x * 2, objects[i]);
                number++;
            }
            else
            {
                CreateChannel();
            }
        }

        GC.Collect();
        radio.SendTwoWay<int, int>(1).Length.Is(number);

        radio.OpenTwoWay<int, int>(x => x * 2, new object());
        radio.SendTwoWay<int, int>(1).Length.Is(number + 1);

        GC.KeepAlive(objects);
    }
}
