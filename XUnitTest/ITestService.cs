// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using Xunit;

namespace XUnitTest;

[RadioServiceInterface]
public interface ITestService : IRadioService
{
    RadioResult<int> Double(int x);
}

public class TestService : ITestService
{
    RadioResult<int> ITestService.Double(int x) => new(x * 2);
}
