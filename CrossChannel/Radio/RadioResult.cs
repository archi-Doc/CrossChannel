// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
{
    // resultArray: null -> Empty, Length == 0 (Array.Empty<T>()) -> Single, Length > 1 -> Array.
    // A zero-length array is never stored as a real result, so it can safely be used as the "single" marker.
    private static readonly T[] SingleMarker = Array.Empty<T>();

    private readonly T result;
    private readonly T[]? resultArray;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioResult{T}"/> struct with a single result.
    /// </summary>
    /// <param name="result">The single result.</param>
    public RadioResult(T result)
    {
        this.result = result;
        this.resultArray = SingleMarker;
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
            this.resultArray = SingleMarker;
        }
        else
        {
            this.result = default!;
            this.resultArray = resultArray;
        }
    }

    /// <summary>
    /// Gets an empty <see cref="RadioResult{T}"/>.
    /// </summary>
    public static RadioResult<T> Empty => default;

    /// <summary>
    /// Gets a value indicating whether the <see cref="RadioResult{T}"/> is empty.
    /// </summary>
    [MemberNotNullWhen(false, nameof(resultArray))]
    public bool IsEmpty => this.resultArray is null;

    /// <summary>
    /// Gets the number of results in the <see cref="RadioResult{T}"/>.
    /// </summary>
    public int Count => this.resultArray is null ? 0 : (this.resultArray.Length == 0 ? 1 : this.resultArray.Length);

    /// <summary>
    /// Creates a <see cref="RadioResult{T}"/> that holds a single result.<br/>
    /// Use this method instead of the constructor when the constructor overload is ambiguous
    /// (e.g. <typeparamref name="T"/> is a reference type and the value is <c>null</c>, or <typeparamref name="T"/> is an array type).
    /// </summary>
    /// <param name="value">The single result.</param>
    /// <returns>A <see cref="RadioResult{T}"/> with a single result.</returns>
    public static RadioResult<T> Single(T value)
        => new RadioResult<T>(value);

    /// <summary>
    /// Creates a <see cref="RadioResult{T}"/> from an array of results.<br/>
    /// An empty array becomes an empty result, and an array with a single element becomes a single result.
    /// </summary>
    /// <param name="array">The array of results.</param>
    /// <returns>A <see cref="RadioResult{T}"/> with the specified results.</returns>
    public static RadioResult<T> FromArray(T[] array)
        => new RadioResult<T>(array);

    /// <summary>
    /// Tries to get the single result from the <see cref="RadioResult{T}"/>.<br/>
    /// If the <see cref="RadioResult{T}"/> holds multiple results, the first one is returned.
    /// </summary>
    /// <param name="result">The single result (the first one if multiple results are held).</param>
    /// <returns><c>true</c> if the <see cref="RadioResult{T}"/> is not empty and a result is retrieved; otherwise, <c>false</c>.</returns>
    public bool TryGetSingleResult([MaybeNullWhen(false)] out T result)
    {
        if (this.resultArray is null)
        {
            result = default!;
            return false;
        }
        else if (this.resultArray.Length == 0)
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
        if (this.resultArray is null)
        {// 0: Empty
            return other.resultArray is null;
        }
        else if (this.resultArray.Length == 0)
        {// 1: Single
            return other.resultArray is { Length: 0 } &&
                EqualityComparer<T>.Default.Equals(this.result, other.result);
        }
        else
        {// >1: Array
            return other.resultArray is { Length: > 1 } &&
                this.resultArray.AsSpan().SequenceEqual(other.resultArray.AsSpan());
        }
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is RadioResult<T> other && this.Equals(other);

    /// <summary>
    /// Determines whether two <see cref="RadioResult{T}"/> instances are equal.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(RadioResult<T> left, RadioResult<T> right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="RadioResult{T}"/> instances are not equal.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><c>true</c> if the instances are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(RadioResult<T> left, RadioResult<T> right) => !left.Equals(right);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (this.resultArray is null)
        {// 0: Empty
            return 0;
        }
        else if (this.resultArray.Length == 0)
        {// 1: Single
            return this.result is null ? 0 : EqualityComparer<T>.Default.GetHashCode(this.result);
        }
        else
        {// >1: Array
            var hash = default(HashCode);
            foreach (var item in this.resultArray)
            {
                hash.Add(item);
            }

            return hash.ToHashCode();
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (this.resultArray is null)
        {// 0: Empty
            return "[]";
        }
        else if (this.resultArray.Length == 0)
        {// 1: Single
            return $"[{this.result?.ToString()}]";
        }
        else
        {// >1: Array
            var sb = new StringBuilder();
            sb.Append('[');
            for (var i = 0; i < this.resultArray.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(this.resultArray[i]?.ToString());
            }

            sb.Append(']');
            return sb.ToString();
        }
    }

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
            if (this.index >= this.total)
            {
                this.current = default(T);
                return false;
            }

            this.current = this.total == 1 ? this.result.result : this.result.resultArray![this.index];
            this.index++;
            return true;
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
                    throw new InvalidOperationException();
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
