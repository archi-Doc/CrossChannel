// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using CrossChannel;
using Xunit;

namespace XUnitTest;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1214 // Readonly fields should appear before non-readonly fields
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS0649

public class CopyTestClass
{
    public int X { get; set; }

    public int Y { get; private set; }

    public int Z { get; init; }

    public string A = string.Empty;

    protected double B;

    private string C = string.Empty;

    private readonly string D = string.Empty;

    private readonly object E;

    private readonly object F;

    private readonly long G;

    private readonly byte[] H;

    public void Prepare()
    {
        this.X = 1;
        this.Y = 2;
        typeof(CopyTestClass).GetMethod("set_Z", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(this, [3]);
        this.A = "3";
        this.B = 4.44;
        this.C = "5";
        Unsafe.AsRef<string>(in this.D) = "99";
        Unsafe.AsRef<object>(in this.E) = 2.55d;
        Unsafe.AsRef<object>(in this.F) = this;
        Unsafe.AsRef<long>(in this.G) = 1234;
        Unsafe.AsRef<byte[]>(in this.H) = [1, 2, 3, 44, 55, 66, 77, 222, 9];
    }

    public bool Compare(CopyTestClass other)
    {
        return this.X == other.X
            && this.Y == other.Y
            && this.Z == other.Z
            && this.A == other.A
            && this.B == other.B
            && this.C == other.C
            && this.D == other.D
            && this.E == other.E
            && this.F == other.F
            && this.G == other.G
            && this.H.AsSpan().SequenceEqual(other.H);
    }
}

public class GhostCopyTest
{
    [Fact]
    public void Test1()
    {
        var tc = new CopyTestClass();
        tc.Prepare();

        var tc2 = new CopyTestClass();
        GhostCopy.Copy(ref tc, ref tc2);
        tc.Compare(tc2).IsTrue();
    }
}
