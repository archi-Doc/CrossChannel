// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using CrossChannel;
using Xunit;

namespace XUnitTest;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1214 // Readonly fields should appear before non-readonly fields

public class CopyTestClass
{
    public int X { get; set; }

    public int Y { get; private set; }

    public int Z { get; init; }

    public string A = string.Empty;

    protected double B;

    private string C = string.Empty;

    private readonly string D = string.Empty;

    public void Prepare()
    {
        this.X = 1;
        this.Y = 2;
        typeof(CopyTestClass).GetMethod("set_Z", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(this, [3]);
        this.A = "3";
        this.B = 4.44;
        this.C = "5";
        Unsafe.AsRef<string>(in this.D) = "99";
    }

    public bool Compare(CopyTestClass other)
    {
        return this.X == other.X
            && this.Y == other.Y
            && this.Z == other.Z
            && this.A == other.A
            && this.B == other.B
            && this.C == other.C
            && this.D == other.D;
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
