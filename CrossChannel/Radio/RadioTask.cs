// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;

namespace CrossChannel;

/// <summary>
/// Helpers used by the generated broker code to build the result of an asynchronous message. Not intended to be called directly.
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
    /// Completes with the aggregated result once every receiver task has completed.<br/>
    /// Unlike 'await', which rethrows only the first exception, it passes on the exceptions of every faulted receiver, in receiver order.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    private sealed class AggregatePromise<T> : TaskCompletionSource<RadioResult<T>>
    {
        private readonly Task<RadioResult<T>>[] tasks;
        private readonly int count;
        private readonly Task whenAll;

        public AggregatePromise(Task<RadioResult<T>>[] tasks, int count)
        {
            this.tasks = tasks;
            this.count = count;

            // The results are read from the tasks themselves, so the non-generic Task.WhenAll needs no result array.
            // It throws ArgumentException for a null task.
            this.whenAll = System.Threading.Tasks.Task.WhenAll(new ReadOnlySpan<Task>(tasks, 0, count)); // 'Task' is the property of TaskCompletionSource here.
            if (this.whenAll.IsCompleted)
            {// Faulted or canceled: complete synchronously, as 'await' does.
                this.Complete();
            }
            else
            {
                this.whenAll.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(this.Complete);
            }
        }

        private void Complete()
        {
            try
            {
                var tasks = new ReadOnlySpan<Task<RadioResult<T>>>(this.tasks, 0, this.count);
                if (this.whenAll.IsCompletedSuccessfully)
                {
                    TryAggregate(tasks, out var result);
                    this.TrySetResult(result);
                }
                else if (this.whenAll.IsFaulted)
                {// Task.WhenAll collects the exceptions in completion order; pass them on in receiver order instead.
                    _ = this.whenAll.Exception; // Observe the fault, so that the finalizer of Task.WhenAll does not raise TaskScheduler.UnobservedTaskException.
                    var exceptions = new List<Exception>();
                    foreach (var x in tasks)
                    {
                        if (x.IsFaulted)
                        {
                            exceptions.AddRange(x.Exception!.InnerExceptions);
                        }
                    }

                    this.TrySetException(exceptions);
                }
                else
                {// Canceled (no receiver faulted): pass on the first canceled receiver, keeping its token and exception.
                    foreach (var x in tasks)
                    {
                        if (x.IsCanceled)
                        {
                            this.TrySetFromTask(x);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {// An exception must not escape a continuation.
                this.TrySetException(ex);
            }
        }
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
    /// Aggregates the tasks of several receivers into a single <see cref="RadioResult{T}"/>, in receiver order.<br/>
    /// Empty results are skipped; only the first value of each nonempty result is collected.<br/>
    /// When every task has already completed successfully, the result is built synchronously.
    /// Otherwise the returned task completes once every task has completed, and passes on the exceptions of every faulted receiver
    /// in receiver order, or the cancellation if no receiver faulted.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="tasks">The array holding the task of every receiver. It is retained until the returned task completes, so it must not be modified.</param>
    /// <param name="count">The number of tasks at the beginning of <paramref name="tasks"/>.</param>
    /// <returns>A task holding the aggregated <see cref="RadioResult{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tasks"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative or greater than the length of <paramref name="tasks"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="tasks"/> contains a null task.</exception>
    public static Task<RadioResult<T>> AggregateAsync<T>(Task<RadioResult<T>>[] tasks, int count)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        if (TryAggregate(new ReadOnlySpan<Task<RadioResult<T>>>(tasks, 0, count), out var result))
        {
            return result.IsEmpty ? GetEmptyResultTask<T>() : Task.FromResult(result);
        }

        // Pending, faulted or canceled.
        return new AggregatePromise<T>(tasks, count).Task;
    }

    /// <summary>
    /// Aggregates the results of several receivers into a single <see cref="RadioResult{T}"/>, in receiver order.<br/>
    /// Kept for binary compatibility: brokers generated by CrossChannel 3.1.0 call this overload.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="resultsTask">A task holding the result of every receiver.</param>
    /// <returns>A task holding the aggregated <see cref="RadioResult{T}"/>.</returns>
    public static async Task<RadioResult<T>> AggregateAsync<T>(Task<RadioResult<T>[]> resultsTask)
    {
        var results = await resultsTask.ConfigureAwait(false);

        var count = 0;
        foreach (var x in results)
        {
            if (!x.IsEmpty)
            {
                count++;
            }
        }

        return Aggregate<RadioResult<T>, ResultReader<T>, T>(results, count);
    }

    /// <summary>
    /// Aggregates the results of the tasks, if every task has already completed successfully.
    /// </summary>
    private static bool TryAggregate<T>(ReadOnlySpan<Task<RadioResult<T>>> tasks, out RadioResult<T> result)
    {
        var count = 0;
        foreach (var x in tasks)
        {
            if (x is null || !x.IsCompletedSuccessfully)
            {
                result = default;
                return false;
            }

            if (!x.Result.IsEmpty)
            {
                count++;
            }
        }

        result = Aggregate<Task<RadioResult<T>>, TaskReader<T>, T>(tasks, count);
        return true;
    }

    /// <summary>
    /// Collects the first value of each nonempty result.
    /// </summary>
    /// <typeparam name="TSource">The type of the sources.</typeparam>
    /// <typeparam name="TReader">The reader of the sources.</typeparam>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="sources">The results, or the tasks holding them.</param>
    /// <param name="count">The number of nonempty results (counted first, so that sparse responses need only one correctly sized array).</param>
    /// <returns>The aggregated <see cref="RadioResult{T}"/>.</returns>
    private static RadioResult<T> Aggregate<TSource, TReader, T>(ReadOnlySpan<TSource> sources, int count)
        where TReader : struct, IResultReader<TSource, T>
    {
        if (count == 0)
        {
            return default;
        }

        var results = count > 1 ? new T[count] : null;
        var index = 0;
        foreach (var x in sources)
        {
            if (TReader.Read(x).TryGetFirst(out var value))
            {
                if (results is null)
                {
                    return new(value);
                }

                results[index++] = value;
            }
        }

        return new(results!);
    }

    /// <summary>
    /// Reads the <see cref="RadioResult{T}"/> of a source, so that results and completed tasks share one aggregation
    /// (the reader is a struct, so the JIT specializes the code for each source without any indirection).
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="T">The type of the result.</typeparam>
    private interface IResultReader<TSource, T>
    {
        static abstract RadioResult<T> Read(TSource source);
    }

    private readonly struct ResultReader<T> : IResultReader<RadioResult<T>, T>
    {
        public static RadioResult<T> Read(RadioResult<T> source) => source;
    }

    private readonly struct TaskReader<T> : IResultReader<Task<RadioResult<T>>, T>
    {
        public static RadioResult<T> Read(Task<RadioResult<T>> source) => source.Result;
    }
}
