// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
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

public readonly struct HybridResult<T> : System.Collections.IEnumerable, IEnumerable<T>
{
    private const ulong SingleResultValue = 0x0000_0000_0000_0001;

    private readonly T result;
    private readonly T[]? resultArray; // 0:Empty, 1:Single, >1:Valid array

    public HybridResult(T result)
    {
        this.result = result;
        Unsafe.As<T[]?, ulong>(ref this.resultArray) = SingleResultValue;
    }

    public HybridResult(T[] resultArray)
    {
        this.result = default!;
        this.resultArray = resultArray;
    }

    [MemberNotNullWhen(false, nameof(resultArray))]
    public bool IsEmpty => this.resultArray is null;

    public int NumberOfResults => this.resultArray is null ? 0 : (this.HasSingleResult ? 1 : this.resultArray.Length);

    public bool TryGetSingleResult([MaybeNullWhen(false)] out T result)
    {
        if (this.IsEmpty)
        {
            result = default!;
            return false;
        }
        else if (this.HasSingleResult)
        {
            result = this.result;
            return true;
        }
        else
        {
            result = this.resultArray[0];
            return true;
        }
    }

#pragma warning disable CS9195 // Argument should be passed with the 'in' keyword
    private bool HasSingleResult => Unsafe.As<T[]?, ulong>(ref Unsafe.AsRef(this.resultArray)) == SingleResultValue;
#pragma warning restore CS9195 // Argument should be passed with the 'in' keyword

    #region Enumerator

    public Enumerator GetEnumerator() => new Enumerator(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private HybridResult<T> result;
        private int index;
        private int total;
        private T? current;

        internal Enumerator(HybridResult<T> result)
        {
            this.result = result;
            this.index = 0;
            this.total = result.NumberOfResults;
            this.current = default(T);
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (this.total == 0)
            {// 0: Empty
                return false;
            }
            else if (this.total == 1)
            {// 1: Single
                if (this.index == 0)
                {
                    this.current = this.result.result;
                    this.index = 1;
                    return true;
                }
                else
                {
                    this.current = default(T);
                    return false;
                }
            }
            else
            {// >1: Array
                if (this.index < this.total)
                {
                    this.current = this.result.resultArray![this.index];
                    this.index++;
                    return true;
                }
                else
                {
                    this.current = default(T);
                    return false;
                }
            }
        }

        public T Current => this.current!;

        object IEnumerator.Current
        {
            get
            {
                if (this.index == 0 || this.index > this.total)
                {
                    throw new IndexOutOfRangeException();
                }

                return this.Current!;
            }
        }

        void IEnumerator.Reset()
        {
            this.index = 0;
            this.current = default(T);
        }
    }

    #endregion
}

[Config(typeof(BenchmarkConfig))]
public class RadioResultTest
{
    public RadioResultTest()
    {
        var result = default(HybridResult<int>);
        var array = result.ToArray();
        result = new HybridResult<int>(22);
        array = result.ToArray();
        result = new HybridResult<int>([11, 22]);
        array = result.ToArray();
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

    [Benchmark]
    public HybridResult<int> Test_Hybrid() => this.HybridMethod(1, 2);

    [Benchmark]
    public int Test_Hybrid2()
    {
        this.HybridMethod(1, 2).TryGetSingleResult(out var result);
        return result;
    }

    private HybridResult<int> HybridMethod(int x, int y) => new(x + y);
}
