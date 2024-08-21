// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// Represents a base interface used as a Radio service.<br/>
/// The requirements are to add the <see cref="RadioServiceInterfaceAttribute" /> and to derive from the <see cref="IRadioService" />.<br/>
/// The return type of the interface function must be either <see cref="void"/>, <see cref="Task"/>, <see cref="RadioResult{T}"/>, <see cref="Task{T}"/>(where TResult is <see cref="RadioResult{T}"/>).
/// </summary>
public interface IRadioService
{
}
