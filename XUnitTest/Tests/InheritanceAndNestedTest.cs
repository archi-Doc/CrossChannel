// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using CrossChannel;
using Xunit;

namespace XUnitTest;

/// <summary>
/// A base interface which is not itself a radio service.
/// </summary>
public interface IPlainBase : IRadioService
{
    void Plain(int x);
}

[RadioService]
public interface IBaseRadioService : IRadioService
{
    void Base(int x);

    RadioResult<int> BaseValue();
}

[RadioService]
public interface IDerivedRadioService : IBaseRadioService, IPlainBase
{
    void Derived(int x);

    Task<RadioResult<int>> DerivedValue();
}

public class DerivedRadioService : IDerivedRadioService
{
    public int PlainSum { get; private set; }

    public int BaseSum { get; private set; }

    public int DerivedSum { get; private set; }

    void IPlainBase.Plain(int x) => this.PlainSum += x;

    void IBaseRadioService.Base(int x) => this.BaseSum += x;

    RadioResult<int> IBaseRadioService.BaseValue() => new(this.BaseSum);

    void IDerivedRadioService.Derived(int x) => this.DerivedSum += x;

    async Task<RadioResult<int>> IDerivedRadioService.DerivedValue()
    {
        await Task.Yield();
        return new(this.DerivedSum);
    }
}

public partial class NestedHost
{
    public partial class Inner
    {
        [RadioService]
        public interface INestedService : IRadioService
        {
            RadioResult<int> Triple(int x);

            Task NoOp();
        }

        public class NestedService : INestedService
        {
            RadioResult<int> INestedService.Triple(int x) => new(x * 3);

            Task INestedService.NoOp() => Task.CompletedTask;
        }
    }
}

public class InheritanceAndNestedTest
{
    [Fact]
    public async Task InheritedMethodsAreBrokered()
    {
        var radio = new RadioClass();
        var service = new DerivedRadioService();

        using (radio.Open<IDerivedRadioService>(service))
        {
            var broker = radio.Send<IDerivedRadioService>();

            broker.Derived(1);
            service.DerivedSum.Is(1);

            // Declared by IBaseRadioService.
            broker.Base(2);
            service.BaseSum.Is(2);
            broker.BaseValue().SequenceEqual([2,]).IsTrue();

            // Declared by IPlainBase (which is not a radio service itself).
            broker.Plain(3);
            service.PlainSum.Is(3);

            (await broker.DerivedValue()).SequenceEqual([1,]).IsTrue();
        }
    }

    [Fact]
    public void TheBaseServiceHasItsOwnChannel()
    {
        var radio = new RadioClass();
        var derived = new DerivedRadioService();
        var @base = new DerivedRadioService();

        using (radio.Open<IDerivedRadioService>(derived))
        using (radio.Open<IBaseRadioService>(@base))
        {
            radio.Send<IBaseRadioService>().Base(5);
            @base.BaseSum.Is(5);
            derived.BaseSum.Is(0); // A different channel.

            radio.Send<IDerivedRadioService>().Base(7);
            derived.BaseSum.Is(7);
            @base.BaseSum.Is(5);
        }
    }

    [Fact]
    public async Task NestedService()
    {
        var radio = new RadioClass();

        radio.Send<NestedHost.Inner.INestedService>().Triple(1).IsEmpty.IsTrue();

        using (radio.Open<NestedHost.Inner.INestedService>(new NestedHost.Inner.NestedService()))
        using (radio.Open<NestedHost.Inner.INestedService>(new NestedHost.Inner.NestedService()))
        {
            radio.Send<NestedHost.Inner.INestedService>().Triple(2).SequenceEqual([6, 6,]).IsTrue();
            await radio.Send<NestedHost.Inner.INestedService>().NoOp();
        }
    }

    [Fact]
    public void GlobalNamespaceService()
    {
        var radio = new RadioClass();
        var service = new ConductorPresentationService();

        using (radio.Open<IConductorPresentationService>(service))
        {
            radio.Send<IConductorPresentationService>().ActivateWindow(true);
            service.Count.Is(1);
        }
    }

    [Fact]
    public async Task ResultOfAnArbitraryType()
    {// RadioResult<Task<int>>: T is not restricted.
        var radio = new RadioClass();

        using (radio.Open<ITestInterface>(new TestInterface()))
        {
            var result = radio.Send<ITestInterface>().Triple(3);
            result.Count.Is(1);
            result.TryGetSingleResult(out var task).IsTrue();
            (await task!).Is(9);
        }
    }

    [Fact]
    public async Task UnsignedResult()
    {
        var radio = new RadioClass();

        using (radio.Open<ITestInterface>(new TestInterface()))
        {
            (await radio.Send<ITestInterface>().Double(5ul)).SequenceEqual([0ul,]).IsTrue();
        }
    }
}
