// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using Arc.Collections;

namespace CrossChannel;

/// <summary>
/// Represents an interface specific to unordered map with Lock.
/// </summary>
public interface IUnorderedMapWithLock
{
    Lock LockObject { get; }

    /// <summary>
    /// Clears the map.
    /// </summary>
    void Clear();

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
internal class UnorderedMapWithLock<TKey, TValue> : UnorderedMap<TKey, TValue>, IUnorderedMapWithLock
{
    public Lock LockObject { get; } = new Lock();
}
