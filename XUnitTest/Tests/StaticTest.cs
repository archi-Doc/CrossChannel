// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using CrossChannel;
using Xunit;

namespace XUnitTest;

public class StaticTest
{ // I prefer static Radio class. but because it's a static class, it causes testing problems.
   // So this is the only test code for the static class.

    [Fact]
    public void Static_TwoWay()
    {
        using (var c = Radio.OpenTwoWay<int, int>(x => x * 2))
        {
            Radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { 2 });
            Radio.SendTwoWay<int, int>(2).IsStructuralEqual(new int[] { 4 });

            using (var d = Radio.OpenTwoWay<int, int>(x => x * 2))
            {
                Radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { 2, 2 });
                Radio.SendTwoWay<int, int>(2).IsStructuralEqual(new int[] { 4, 4 });
            }

            Radio.SendTwoWay<int, int>(3).IsStructuralEqual(new int[] { 6 });
        }

        Radio.SendTwoWay<int, int>(4).IsStructuralEqual(new int[] { });
    }

    [Fact]
    public void Static_TwoWayKey()
    {
        using (var c = Radio.OpenTwoWayKey<int, int, int>(1, x => x * 2))
        {
            Radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });
            Radio.SendTwoWayKey<int, int, int>(0, 2).IsStructuralEqual(new int[] { });
            Radio.SendTwoWayKey<int, int, int>(1, 2).IsStructuralEqual(new int[] { 4 });

            using (var d = Radio.OpenTwoWayKey<int, int, int>(2, x => x * 3))
            {
                Radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });
                Radio.SendTwoWayKey<int, int, int>(0, 2).IsStructuralEqual(new int[] { });
                Radio.SendTwoWayKey<int, int, int>(1, 2).IsStructuralEqual(new int[] { 4 });
                Radio.SendTwoWayKey<int, int, int>(2, 2).IsStructuralEqual(new int[] { 6 });
            }
        }

        Radio.SendTwoWay<int, int>(4).IsStructuralEqual(new int[] { });
    }
}
