// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using CrossChannel;
using Xunit;

namespace XUnitTest
{
    public class CleanupTest
    {
        [Fact]
        public void Test1()
        {
            var radio = new RadioClass();

            void CreateChannel()
            {
                radio.OpenTwoWay<int, int>(x => x * 2, new object());
            }

            radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });

            for (var i = 0; i < CrossChannelConst.CleanupListThreshold; i++)
            {
                CreateChannel();
            }

            GC.Collect(); // Empty list

            for (var i = 0; i < CrossChannelConst.CleanupListThreshold; i++)
            {
                if (i % 3 == 0)
                {
                    radio.OpenTwoWay<int, int>(x => x * 2, new object());
                }
                else
                {
                    CreateChannel();
                }
            }

            GC.Collect(); // Empty list
            CreateChannel();
        }
    }
}
