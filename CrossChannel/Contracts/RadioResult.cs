// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// A structure that represents the return value of a radio message.<br/>
/// The return value on the receiving side (the processing side) is singular, <br/>
/// but since the number of return values on the sending side can be zero or more, <br/>
/// please use this structure for the return value.
/// </summary>
/// <typeparam name="T">The type of the message.</typeparam>
public readonly struct HybridResult<T> : IEnumerable, IEnumerable<T>, IEquatable<HybridResult<T>>
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
        if (resultArray.Length == 0)
        {
            this.result = default!;
            this.resultArray = null;
        }
        else if (resultArray.Length == 1)
        {
            this.result = resultArray[0];
            Unsafe.As<T[]?, ulong>(ref this.resultArray) = SingleResultValue;
        }
        else
        {
            this.result = default!;
            this.resultArray = resultArray;
        }
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

    public bool Equals(HybridResult<T> other)
    {
        if (this.IsEmpty)
        {// 0: Empty
            return other.IsEmpty;
        }
        else if (this.HasSingleResult)
        {// 1: Single
            return other.HasSingleResult && EqualityComparer<T>.Default.Equals(this.result, other.result);
        }
        else
        {// >1: Array
            return other.resultArray != null && this.resultArray!.SequenceEqual(other.resultArray!);
        }
    }

    public override int GetHashCode()
    {
        if (this.IsEmpty)
        {// 0: Empty
            return 0;
        }
        else if (this.HasSingleResult)
        {// 1: Single
            return this.result!.GetHashCode();
        }
        else
        {// >1: Array
            var hash = 0;
            foreach (var item in this.resultArray!)
            {
                hash ^= item!.GetHashCode();
            }

            return hash;
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
