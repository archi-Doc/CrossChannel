// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// The marker interface every radio service must derive from.<br/>
/// The interface must also carry the <see cref="RadioServiceAttribute"/>, and the return type of each of its abstract or virtual
/// methods must be <see langword="void"/>, <see cref="Task"/>, <see cref="RadioResult{T}"/>,
/// or <see cref="Task{TResult}"/> of <see cref="RadioResult{T}"/>.
/// </summary>
public interface IRadioService
{
}
