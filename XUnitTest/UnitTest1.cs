// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using CrossChannel;
using Xunit;

namespace XUnitTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
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
    }
}
