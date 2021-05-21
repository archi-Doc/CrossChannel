// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using CrossChannel;
using Xunit;

namespace XUnitTest
{
    public class WeakReferenceTest
    {
        [Fact]
        public void WeakDelegate_Lambda()
        {
            var radio = new RadioClass();

            void CreateChannel()
            {
                radio.OpenTwoWay<int, int>(x => x * 2, new object());
                radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { 2 });
            }

            radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });

            CreateChannel();

            GC.Collect();
            radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });
        }

        [Fact]
        public void WeakDelegate_Method()
        {
            var radio = new RadioClass();

            radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });

            new InternalClass(radio);

            GC.Collect();
            radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });
        }

        private class InternalClass
        {
            public InternalClass(RadioClass radio)
            {
                radio.OpenTwoWay<int, int>(this.Function, new object());
                radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { 2 });
            }

            public int Function(int x) => x * 2;
        }

        [Fact]
        public void WeakDelegate_Static()
        {
            var radio = new RadioClass();

            void CreateChannel()
            {
                radio.OpenTwoWay<int, int>(StaticFunction, new object());
                radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { 2 });
            }

            radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });

            CreateChannel();

            GC.Collect();
            radio.SendTwoWay<int, int>(1).IsStructuralEqual(new int[] { });
        }

        private static int StaticFunction(int x) => x * 2;
    }
}
