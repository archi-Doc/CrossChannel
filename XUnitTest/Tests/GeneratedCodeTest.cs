// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using CrossChannel;
using Xunit;

namespace XUnitTest;

// Service shapes which the generated broker code once failed to compile. The generator runs when this project is built,
// so building it proves that the brokers compile, and the tests below prove that they deliver.

[RadioService]
public partial interface IGeneratedPartialService : IRadioService
{
    RadioResult<int> Multiply(int x);
}

public partial interface IGeneratedPartialService
{
    void Add(int x);
}

[RadioService(AutoRegisterServiceAndSender = false)] // The broker is not registered as a disposable singleton.
public interface IGeneratedSystemBaseService : IRadioService, IProgress<int>, IDisposable
{
    RadioResult<int> Total();
}

public class GeneratedSystemComponent
{
    public string Name => "System";
}

public partial class GeneratedHost
{
    // Inside this class, 'System' refers to this property instead of the System namespace.
    public GeneratedSystemComponent System { get; } = new();

    [RadioService]
    public interface INestedService : IRadioService
    {
        void Add(int x);

        RadioResult<string> Name();

        Task RunAsync();

        Task<RadioResult<int>> ValueAsync();
    }
}

[RadioService]
public interface IGeneratedNullableTaskService : IRadioService
{
    Task? RunAsync();

    Task<RadioResult<string?>>? TextAsync();
}

[RadioService]
public interface IGeneratedObsoleteService : IRadioService
{
    [Obsolete]
    void Old(int x);

    [Obsolete("Use Current()", true)]
    void Removed();

    RadioResult<int> Current();
}

[RadioService]
public interface IGeneratedDelegateService : IRadioService
{
    RadioResult<Func<int>> Factory();

    Task<RadioResult<Action?>> CallbackAsync();
}

public class GeneratedReceiver : IGeneratedPartialService, IGeneratedSystemBaseService, GeneratedHost.INestedService, IGeneratedNullableTaskService, IGeneratedObsoleteService, IGeneratedDelegateService
{
    private readonly int id;
    private readonly bool returnNull;

    public GeneratedReceiver(int id, bool returnNull = false)
    {
        this.id = id;
        this.returnNull = returnNull;
    }

    public int Sum { get; private set; }

    public int DisposeCalls { get; private set; }

    public int RunCalls { get; private set; }

    public int Callbacks { get; private set; }

    RadioResult<int> IGeneratedPartialService.Multiply(int x) => new(x * this.id);

    void IGeneratedPartialService.Add(int x) => this.Sum += x;

    void IProgress<int>.Report(int value) => this.Sum += value;

    void IDisposable.Dispose() => this.DisposeCalls++;

    RadioResult<int> IGeneratedSystemBaseService.Total() => new(this.Sum);

    void GeneratedHost.INestedService.Add(int x) => this.Sum += x;

    RadioResult<string> GeneratedHost.INestedService.Name() => new(this.id.ToString());

    async Task GeneratedHost.INestedService.RunAsync()
    {
        await Task.Yield();
        this.RunCalls++;
    }

    Task<RadioResult<int>> GeneratedHost.INestedService.ValueAsync() => Task.FromResult(new RadioResult<int>(this.id));

    Task? IGeneratedNullableTaskService.RunAsync()
    {
        if (this.returnNull)
        {
            return null;
        }

        this.RunCalls++;
        return Task.CompletedTask;
    }

    Task<RadioResult<string?>>? IGeneratedNullableTaskService.TextAsync()
        => this.returnNull ? null : Task.FromResult(RadioResult<string?>.FromValue(this.id.ToString()));

    void IGeneratedObsoleteService.Old(int x) => this.Sum += x;

    void IGeneratedObsoleteService.Removed()
    {
    }

    RadioResult<int> IGeneratedObsoleteService.Current() => new(this.id);

    RadioResult<Func<int>> IGeneratedDelegateService.Factory() => RadioResult<Func<int>>.FromValue(() => this.id);

    Task<RadioResult<Action?>> IGeneratedDelegateService.CallbackAsync() => Task.FromResult(RadioResult<Action?>.FromValue(() => this.Callbacks++));
}

public class GeneratedCodeTest
{
    [Fact]
    public void PartialService()
    {
        var radio = new LocalRadio();
        var receivers = new[] { new GeneratedReceiver(1), new GeneratedReceiver(2), };
        var links = receivers.Select(x => radio.Open<IGeneratedPartialService>(x)!).ToArray();

        radio.Send<IGeneratedPartialService>().Multiply(3).SequenceEqual([3, 6,]).IsTrue();
        radio.Send<IGeneratedPartialService>().Add(5); // Declared in the other part.
        receivers.All(x => x.Sum == 5).IsTrue();

        foreach (var x in links)
        {
            x.Dispose();
        }
    }

    [Fact]
    public void ServiceInheritingSystemInterfaces()
    {// The methods of IProgress<int> and IDisposable are brokered as well.
        var radio = new LocalRadio();
        var receivers = new[] { new GeneratedReceiver(1), new GeneratedReceiver(2), };
        var links = receivers.Select(x => radio.Open<IGeneratedSystemBaseService>(x)!).ToArray();

        var broker = radio.Send<IGeneratedSystemBaseService>();
        broker.Report(4);
        broker.Report(3);
        broker.Total().SequenceEqual([7, 7,]).IsTrue();

        broker.Dispose();
        receivers.All(x => x.DisposeCalls == 1).IsTrue();

        foreach (var x in links)
        {
            x.Dispose();
        }
    }

    [Fact]
    public async Task NestedServiceInAHostWithASystemMember()
    {
        var radio = new LocalRadio();
        var receivers = new[] { new GeneratedReceiver(1), new GeneratedReceiver(2), };
        var links = receivers.Select(x => radio.Open<GeneratedHost.INestedService>(x)!).ToArray();

        var broker = radio.Send<GeneratedHost.INestedService>();
        broker.Add(2);
        receivers.All(x => x.Sum == 2).IsTrue();
        broker.Name().SequenceEqual(["1", "2",]).IsTrue();

        await broker.RunAsync();
        receivers.All(x => x.RunCalls == 1).IsTrue();

        var task = broker.ValueAsync();
        task.IsCompletedSuccessfully.IsTrue(); // Every receiver completed synchronously.
        (await task).SequenceEqual([1, 2,]).IsTrue();

        foreach (var x in links)
        {
            x.Dispose();
        }
    }

    [Fact]
    public async Task NullableTaskReturns()
    {// A receiver returning a null task counts as completed without a result.
        var radio = new LocalRadio();

        using (radio.Open<IGeneratedNullableTaskService>(new GeneratedReceiver(0, true)))
        {
            var task = radio.Send<IGeneratedNullableTaskService>().RunAsync();
            task.IsNotNull();
            task!.IsCompletedSuccessfully.IsTrue();

            var task2 = radio.Send<IGeneratedNullableTaskService>().TextAsync();
            ReferenceEquals(task2, RadioTask.GetEmptyResultTask<string?>()).IsTrue();

            var receiver = new GeneratedReceiver(1);
            using (radio.Open<IGeneratedNullableTaskService>(receiver))
            using (radio.Open<IGeneratedNullableTaskService>(new GeneratedReceiver(0, true)))
            using (radio.Open<IGeneratedNullableTaskService>(new GeneratedReceiver(2)))
            {
                await radio.Send<IGeneratedNullableTaskService>().RunAsync()!;
                receiver.RunCalls.Is(1);

                (await radio.Send<IGeneratedNullableTaskService>().TextAsync()!).SequenceEqual(["1", "2",]).IsTrue();
            }
        }
    }

    [Fact]
    public void ObsoleteMethods()
    {
        var radio = new LocalRadio();
        var receiver = new GeneratedReceiver(3);

        using (radio.Open<IGeneratedObsoleteService>(receiver))
        {
#pragma warning disable CS0612 // Type or member is obsolete
            radio.Send<IGeneratedObsoleteService>().Old(4);
#pragma warning restore CS0612 // Type or member is obsolete
            receiver.Sum.Is(4);
            radio.Send<IGeneratedObsoleteService>().Current().SequenceEqual([3,]).IsTrue();
        }
    }

    [Fact]
    public async Task DelegateResults()
    {
        var radio = new LocalRadio();
        var receivers = new[] { new GeneratedReceiver(1), new GeneratedReceiver(2), };
        var links = receivers.Select(x => radio.Open<IGeneratedDelegateService>(x)!).ToArray();

        radio.Send<IGeneratedDelegateService>().Factory().Select(x => x()).SequenceEqual([1, 2,]).IsTrue();

        foreach (var x in await radio.Send<IGeneratedDelegateService>().CallbackAsync())
        {
            x!();
        }

        receivers.All(x => x.Callbacks == 1).IsTrue();

        foreach (var x in links)
        {
            x.Dispose();
        }
    }
}
