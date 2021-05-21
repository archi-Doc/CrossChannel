// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using CrossChannel;
using Xunit;

namespace XUnitTest
{
    public class CleanupTest
    {
        [Fact]
        public void Static_TwoWay()
        {
            static void CreateChannel()
            {
                Radio.OpenTwoWay<int, int>(x => x * 2, new object());
            }

            Radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });

            for (var i = 0; i < Radio.Const.CleanupListThreshold; i++)
            {
                CreateChannel();
            }

            GC.Collect(); // Empty list

            for (var i = 0; i < Radio.Const.CleanupListThreshold; i++)
            {
                if (i % 3 == 0)
                {
                    Radio.OpenTwoWay<int, int>(x => x * 2, new object());
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
