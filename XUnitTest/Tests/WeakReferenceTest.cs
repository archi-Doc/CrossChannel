// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using CrossChannel;
using Xunit;

namespace XUnitTest
{
    public class WeakReferenceTest
    {
        [Fact]
        public void Static_TwoWay()
        {
            static void CreateChannel()
            {
                Radio.OpenTwoWay<int, int>(x => x * 2, new object());
            }

            Radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });

            CreateChannel();
            Radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { 2 });

            GC.Collect();
            Radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });
        }
    }
}
