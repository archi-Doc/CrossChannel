// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using Arc.Collections;

namespace CrossChannel;

/// <summary>
/// Represents the non-generic part of a keyed channel map.<br/>
/// A keyed <see cref="Channel{TService}"/> shares <see cref="LockObject"/> with its map,
/// so that the map node and the links of the channel are updated atomically.
/// </summary>
internal interface IUnorderedMapWithLock
{
    /// <summary>
    /// Gets the lock which protects both the map and the channels it contains.
    /// </summary>
    Lock LockObject { get; }

    /// <summary>
    /// Removes the node at the specified index.
    /// </summary>
    /// <param name="nodeIndex">The index of the node to remove.</param>
    void RemoveNode(int nodeIndex);
}

/// <summary>
/// <see cref="UnorderedMap{TKey, TValue}"/> + <see cref="System.Threading.Lock"/>.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
internal sealed class UnorderedMapWithLock<TKey, TValue> : UnorderedMap<TKey, TValue>, IUnorderedMapWithLock
{
    /// <inheritdoc/>
    public Lock LockObject { get; } = new Lock();
}
