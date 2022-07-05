// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using CrossChannel;
using Xunit;

namespace XUnitTest;

public class WeakInterfaceTest
{
    [Fact]
    public void WeakInterface_Test()
    {
        var radio = new RadioClass();

        void CreateChannel()
        {
            var tc1 = new TestClass1();
            var i2 = (ITestInterface)new TestClass2();

            tc1.Register2(radio);
            i2.Register(radio);

            radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { 1, 2 });
        }

        radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });

        CreateChannel();

        GC.Collect();
        radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });
    }

    [Fact]
    public void WeakInterface_Test2()
    {
        var radio = new RadioClass();

        void CreateChannel()
        {
            var tc1 = (ITestInterface)new TestClass1();
            var i2 = (ITestInterface)new TestClass2();

            tc1.Register(radio);
            i2.Register(radio);

            radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { 1, 2 });
        }

        radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });

        CreateChannel();

        GC.Collect();
        radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });
    }

    private interface ITestInterface
    {
        int Multi(int x);

        void Register(RadioClass radio)
        {
            radio.OpenTwoWay<int, int>(a => this.Multi(a), this);
        }
    }

    private class TestClass1 : ITestInterface
    {
        public int Multi(int x) => x * 1;

        public void Register2(RadioClass radio)
        {
            radio.OpenTwoWay<int, int>(a => this.Multi(a), this);
        }
    }

    private class TestClass2 : ITestInterface
    {
        public int Multi(int x) => x * 2;

        public void Register2(RadioClass radio)
        {
            radio.OpenTwoWay<int, int>(a => this.Multi(a), this);
        }
    }
}
