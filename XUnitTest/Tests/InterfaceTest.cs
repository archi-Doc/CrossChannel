// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using CrossChannel;
using Xunit;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace XUnitTest;

[RadioServiceInterface]
public interface ITestInterface : IRadioService
{
    Task<RadioResult<ulong>> Double(ulong x);

    RadioResult<Task<int>> Triple(int x);
}

public class TestInterface : ITestInterface
{
    async Task<RadioResult<ulong>> ITestInterface.Double(ulong x)
    {
        return new(0);
    }

    RadioResult<Task<int>> ITestInterface.Triple(int x)
    {
        return new(Task.FromResult(x * 3));
    }
}
