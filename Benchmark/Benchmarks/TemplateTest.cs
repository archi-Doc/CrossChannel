// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Benchmark;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class TemplateTest
{
    public TemplateTest()
    {
    }

    [Benchmark]
    public void Test1()
    {
    }
}
