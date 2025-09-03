// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using Benchmark;
using BenchmarkDotNet.Attributes;
using CrossChannel;

namespace Benchmark;

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
}

[Config(typeof(BenchmarkConfig))]
public class GhostCopyBenchmark
{
    private CopyTestClass tc = new();
    private CopyTestClass tc2 = new();

    public GhostCopyBenchmark()
    {
        this.tc.Prepare();
        var t = new CopyTestClass();
        GhostCopy.Copy(ref this.tc, ref t);
    }

    [Benchmark]
    public GhostCopy.CopyDelegate<CopyTestClass> CreateDelegate()
    {
        return GhostCopy.CreateDelegate<CopyTestClass>();
    }

    [Benchmark]
    public CopyTestClass Copy()
    {
        GhostCopy.Copy<CopyTestClass>(ref this.tc, ref this.tc2);
        return this.tc2;
    }
}
