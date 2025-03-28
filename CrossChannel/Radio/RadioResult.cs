﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;

namespace CrossChannel;

/// <summary>
/// A structure that represents the return value of a radio message.<br/>
/// The return value on the receiving side (the processing side) is singular, <br/>
/// but since the number of return values on the sending side can be zero or more, <br/>
/// please use this structure for the return value.
/// </summary>
/// <typeparam name="T">The type of the message.</typeparam>
public readonly struct RadioResult<T> : IEnumerable, IEnumerable<T>, IEquatable<RadioResult<T>>
{// It feels a bit forced, but I like this structure.
    private const ulong SingleResultValue = 0x0000_0000_0000_0001;

    private readonly T result;
    private readonly T[]? resultArray; // 0:Empty, 1:Single, >1:Valid array

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioResult{T}"/> struct with a single result.
    /// </summary>
    /// <param name="result">The single result.</param>
    public RadioResult(T result)
    {
        this.result = result;
        Unsafe.As<T[]?, ulong>(ref this.resultArray) = SingleResultValue;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioResult{T}"/> struct with an array of results.
    /// </summary>
    /// <param name="resultArray">The array of results.</param>
    public RadioResult(T[] resultArray)
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

    /// <summary>
    /// Gets a value indicating whether the <see cref="RadioResult{T}"/> is empty.
    /// </summary>
    [MemberNotNullWhen(false, nameof(resultArray))]
    public bool IsEmpty => this.resultArray is null;

    /// <summary>
    /// Gets the number of results in the <see cref="RadioResult{T}"/>.
    /// </summary>
    public int Count => this.resultArray is null ? 0 : (this.HasSingleResult ? 1 : this.resultArray.Length);

    /// <summary>
    /// Tries to get the single result from the <see cref="RadioResult{T}"/>.
    /// </summary>
    /// <param name="result">The single result.</param>
    /// <returns><c>true</c> if the single result is successfully retrieved; otherwise, <c>false</c>.</returns>
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

    /// <summary>
    /// Determines whether the specified <see cref="RadioResult{T}"/> is equal to the current <see cref="RadioResult{T}"/>.
    /// </summary>
    /// <param name="other">The <see cref="RadioResult{T}"/> to compare with the current <see cref="RadioResult{T}"/>.</param>
    /// <returns><c>true</c> if the specified <see cref="RadioResult{T}"/> is equal to the current <see cref="RadioResult{T}"/>; otherwise, <c>false</c>.</returns>
    public bool Equals(RadioResult<T> other)
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override string ToString()
    {
        if (this.IsEmpty)
        {// 0: Empty
            return "[]";
        }
        else if (this.HasSingleResult)
        {// 1: Single
            return $"[{this.result!.ToString()}]";
        }
        else
        {// >1: Array
            var sb = new StringBuilder();
            sb.Append('[');
            foreach (var item in this.resultArray!)
            {
                sb.Append($"{item?.ToString()}, ");
            }

            sb.Append(']');
            return sb.ToString();
        }
    }

#pragma warning disable CS9195 // Argument should be passed with the 'in' keyword
    private bool HasSingleResult => Unsafe.As<T[]?, ulong>(ref Unsafe.AsRef(this.resultArray)) == SingleResultValue;
#pragma warning restore CS9195 // Argument should be passed with the 'in' keyword

    #region Enumerator

    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <inheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// Enumerates the results in the <see cref="RadioResult{T}"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private RadioResult<T> result;
        private int index;
        private int total;
        private T? current;

        internal Enumerator(RadioResult<T> result)
        {
            this.result = result;
            this.index = 0;
            this.total = result.Count;
            this.current = default(T);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public T Current => this.current!;

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        void IEnumerator.Reset()
        {
            this.index = 0;
            this.current = default(T);
        }
    }

    #endregion
}
