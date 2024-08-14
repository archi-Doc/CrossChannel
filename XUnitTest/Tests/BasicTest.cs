// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using CrossChannel;
using Xunit;

namespace XUnitTest;

public class BasicTest
{
    [Fact]
    public void Class_TwoWay()
    {
        var radio = new ObsoleteRadioClass();
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
}
