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
    /// <typeparam name="T">The type of the result.</typeparam>
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
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <returns>A completed task holding an empty <see cref="RadioResult{T}"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<RadioResult<T>> GetEmptyResultTask<T>()
        => EmptyCache<T>.Task;

    /// <summary>
    /// Aggregates the results of several receivers into a single <see cref="RadioResult{T}"/>.<br/>
    /// Empty results are skipped; only the first value of each nonempty result is collected.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="resultsTask">A task holding the result of every receiver.</param>
    /// <returns>The aggregated <see cref="RadioResult{T}"/>.</returns>
    public static async Task<RadioResult<T>> AggregateAsync<T>(Task<RadioResult<T>[]> resultsTask)
    {
        var radioResults = await resultsTask.ConfigureAwait(false);

        var count = 0;
        foreach (var x in radioResults)
        {
            if (!x.IsEmpty)
            {
                count++;
            }
        }

        if (count == 0)
        {
            return default;
        }

        // Count first so sparse responses need only one correctly sized array.
        var results = count > 1 ? new T[count] : null;
        var index = 0;
        foreach (var x in radioResults)
        {
            if (x.TryGetFirst(out var result))
            {
                if (results is null)
                {
                    return new(result);
                }

                results[index++] = result;
            }
        }

        return new(results!);
    }
}
