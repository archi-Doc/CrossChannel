// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using CrossChannel;
using Xunit;

namespace XUnitTest
{
    public class BasicTest
    {
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
        public void Class_TwoWay()
        {
            var radio = new RadioClass();
            using (var c = radio.OpenTwoWay<int, int>(x => x * 2))
            {
                radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { 2 });
                radio.SendTwoWay<int, int>(2).IsStructuralEqual(new int[] { 4 });

                using (var d = radio.OpenTwoWay<int, int>(x => x * 2))
                {
                    radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { 2, 2 });
                    radio.SendTwoWay<int, int>(2).IsStructuralEqual(new int[] { 4, 4 });
                }

                radio.SendTwoWay<int, int>(3).IsStructuralEqual(new int[] { 6 });
            }

            radio.SendTwoWay<int, int>(4).IsStructuralEqual(new int[] { });
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
}
