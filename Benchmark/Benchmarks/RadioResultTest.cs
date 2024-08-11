// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Benchmark;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

public readonly struct SingleResult<T>
{
    public readonly T Result;

    public SingleResult(T result)
    {
        this.Result = result;
    }
}

public readonly struct ArrayResult<T>
{
    public readonly T[] ResultArray;

    public ArrayResult(T result)
    {
        this.ResultArray = [result];
    }

    public int NumberOfResults => this.ResultArray.Length;
}

[Config(typeof(BenchmarkConfig))]
public class RadioResultTest
{
    public RadioResultTest()
    {
    }

    [Benchmark]
    public int Test_Direct() => this.DirectMethod(1, 2);

    private int DirectMethod(int x, int y) => x + y;

    [Benchmark]
    public SingleResult<int> Test_Single() => this.SingleMethod(1, 2);

    [Benchmark]
    public int Test_Single2() => this.SingleMethod(1, 2).Result;

    private SingleResult<int> SingleMethod(int x, int y) => new(x + y);

    [Benchmark]
    public ArrayResult<int> Test_Array() => this.ArrayMethod(1, 2);

    [Benchmark]
    public int Test_Array2() => this.ArrayMethod(1, 2).ResultArray[0];

    private ArrayResult<int> ArrayMethod(int x, int y) => new(x + y);
}
