// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;

namespace CrossChannel;

/// <summary>
/// Helpers used by the generated broker code to build the result of an asynchronous message.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RadioTask
{
    /// <summary>
    /// Holds the cached empty task of each result type.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    private static class EmptyCache<T>
    {
        // A field initializer (instead of a static constructor) keeps the type 'beforefieldinit',
        // so the JIT can elide the class initialization check on the hot path.
        public static readonly Task<RadioResult<T>> Task = System.Threading.Tasks.Task.FromResult<RadioResult<T>>(default);
    }

    /// <summary>
    /// Gets a cached, already completed task holding an empty <see cref="RadioResult{T}"/>.<br/>
    /// Used when no receiver responded, so that the common case does not allocate.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    /// <returns>A completed task holding an empty <see cref="RadioResult{T}"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<RadioResult<T>> EmptyResult<T>()
        => EmptyCache<T>.Task;

    /// <summary>
    /// Aggregates the results of several receivers into a single <see cref="RadioResult{T}"/>.<br/>
    /// Empty results are skipped.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    /// <param name="resultsTask">A task holding the result of every receiver.</param>
    /// <returns>The aggregated <see cref="RadioResult{T}"/>.</returns>
    public static async Task<RadioResult<T>> Aggregate<T>(Task<RadioResult<T>[]> resultsTask)
    {
        var radioResults = await resultsTask.ConfigureAwait(false);

        var firstResult = default(T)!;
        T[]? results = default;
        var count = 0;
        foreach (var x in radioResults)
        {
            if (!x.TryGetSingleResult(out var r))
            {
                continue;
            }

            if (count == 0)
            {
                firstResult = r;
            }
            else
            {
                if (results is null)
                {
                    results = new T[radioResults.Length];
                    results[0] = firstResult;
                }

                results[count] = r;
            }

            count++;
        }

        if (count == 0)
        {
            return default;
        }
        else if (count == 1)
        {
            return new(firstResult);
        }
        else if (results!.Length != count)
        {
            Array.Resize(ref results, count);
        }

        return new(results!);
    }
}
