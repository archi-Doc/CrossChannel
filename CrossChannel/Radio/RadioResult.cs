// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;

namespace CrossChannel;

/// <summary>
/// Holds the results of a radio message: zero or more values, collected from the receivers in order.<br/>
/// Each receiver should return zero or one value, since a broker may keep only the first value of each receiver.
/// Zero or one value is held without allocation.
/// </summary>
/// <typeparam name="T">The type of the result.</typeparam>
public readonly struct RadioResult<T> : IEnumerable, IEnumerable<T>, IEquatable<RadioResult<T>>
{
    // resultArray: null -> Empty, Length == 0 (Array.Empty<T>()) -> Single, Length > 1 -> Array.
    // A zero-length array is never stored as a real result, so it can safely be used as the "single" marker.
    private static readonly T[] SingleMarker = Array.Empty<T>();

    private readonly T result;
    private readonly T[]? resultArray;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioResult{T}"/> struct with a single result.<br/>
    /// Use <see cref="FromValue(T)"/> instead when the value is <see langword="null"/>, <see langword="default"/>, or <c>[]</c>,
    /// or when <typeparamref name="T"/> is a type that arrays convert to (e.g. <see cref="object"/>) and the value is an array.
    /// </summary>
    /// <param name="result">The single result.</param>
    public RadioResult(T result)
    {
        this.result = result;
        this.resultArray = SingleMarker;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioResult{T}"/> struct with an array of results.<br/>
    /// An empty array gives an empty result, and an array with a single element gives a single result.
    /// </summary>
    /// <param name="results">The results. An array with two or more elements is retained without copying and must not be modified afterwards.</param>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is null.</exception>
    public RadioResult(T[] results)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (results.Length == 0)
        {
            this.result = default!;
            this.resultArray = null;
        }
        else if (results.Length == 1)
        {
            this.result = results[0];
            this.resultArray = SingleMarker;
        }
        else
        {
            this.result = default!;
            this.resultArray = results;
        }
    }

    /// <summary>
    /// Gets an empty <see cref="RadioResult{T}"/> (no receiver responded).
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
    /// Creates a <see cref="RadioResult{T}"/> that holds a single result, whatever the value is.<br/>
    /// Use this method when the constructor would not bind to the single-value overload. <c>null</c>, <c>default</c>,
    /// or <c>[]</c> can make the call ambiguous (a compile error). When <typeparamref name="T"/> is a type that arrays convert to
    /// (e.g. <see cref="object"/>, <see cref="Array"/>, <see cref="IEnumerable"/>, or <see cref="IEnumerable{T}"/> of <see cref="object"/>),
    /// <c>null</c>, <c>default</c>, <c>[]</c>, and arrays of a reference type convertible to <typeparamref name="T"/> (e.g. <c>string[]</c>) silently bind to the array overload: the array
    /// elements become separate results, <c>null</c> and <c>default</c> throw <see cref="ArgumentNullException"/>, and <c>[]</c> gives an empty result.
    /// </summary>
    /// <param name="value">The single result.</param>
    /// <returns>A <see cref="RadioResult{T}"/> with a single result.</returns>
    public static RadioResult<T> FromValue(T value)
        => new RadioResult<T>(value);

    /// <summary>
    /// Creates a <see cref="RadioResult{T}"/> from an array of results.<br/>
    /// An empty array becomes an empty result, and an array with a single element becomes a single result.
    /// </summary>
    /// <param name="results">The results. An array with two or more elements is retained without copying and must not be modified afterwards.</param>
    /// <returns>A <see cref="RadioResult{T}"/> with the specified results.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is null.</exception>
    public static RadioResult<T> FromArray(T[] results)
        => new RadioResult<T>(results);

    /// <summary>
    /// Tries to get the first result. This is the usual way to read the response of a single receiver.<br/>
    /// When several receivers responded, only the first result is returned; enumerate the
    /// <see cref="RadioResult{T}"/> to read all of them.
    /// </summary>
    /// <param name="result">When this method returns, contains the first result, if there is at least one.</param>
    /// <returns><see langword="true"/> if a result was retrieved; <see langword="false"/> if the <see cref="RadioResult{T}"/> is empty.</returns>
    public bool TryGetFirst([MaybeNullWhen(false)] out T result)
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
    /// Determines whether this result and <paramref name="other"/> hold the same values in the same order, compared with <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    /// <param name="other">The result to compare with.</param>
    /// <returns><see langword="true"/> if the results are equal; otherwise, <see langword="false"/>.</returns>
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
            // ReadOnlySpan<T>(T[]) has no array covariance check, unlike Span<T> (AsSpan throws for a string[] stored as object[]).
            return other.resultArray is { Length: > 1 } &&
                new ReadOnlySpan<T>(this.resultArray).SequenceEqual(new ReadOnlySpan<T>(other.resultArray));
        }
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is RadioResult<T> other && this.Equals(other);

    /// <summary>
    /// Determines whether two results are equal (see <see cref="Equals(RadioResult{T})"/>).
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the results are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(RadioResult<T> left, RadioResult<T> right) => left.Equals(right);

    /// <summary>
    /// Determines whether two results are not equal (see <see cref="Equals(RadioResult{T})"/>).
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the results are not equal; otherwise, <see langword="false"/>.</returns>
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

    /// <summary>
    /// Returns an enumerator which iterates over the results.
    /// </summary>
    /// <returns>An enumerator.</returns>
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
                this.index = this.total + 1;
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
