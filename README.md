# CrossChannel

A fast, interface-based Pub/Sub library for .NET 10.

[![Nuget](https://img.shields.io/nuget/v/Arc.CrossChannel)](https://www.nuget.org/packages/Arc.CrossChannel) [![Build and Test](https://github.com/archi-Doc/CrossChannel/actions/workflows/test.yml/badge.svg)](https://github.com/archi-Doc/CrossChannel/actions/workflows/test.yml)

- Messages are plain **interface methods**, so the compiler checks every call.
- A **source generator** emits the delivery code, so sending uses no reflection and, in the common cases, allocates nothing.
- Supports **return values**, **asynchronous** methods, and **weak references**.
- **Keyed channels** deliver a message only to the receivers subscribed with the same key.
- A process-wide `Radio`, independent `LocalRadio` instances, standalone channels, and **dependency injection**.
- Thread-safe, trimming-safe, and **Native AOT** compatible.



## Table of Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Cheat sheet](#cheat-sheet)
- [Features](#features)
  - [Return values](#return-values)
  - [Asynchronous methods](#asynchronous-methods)
  - [Weak references](#weak-references)
  - [Keyed channels](#keyed-channels)
  - [Maximum number of links](#maximum-number-of-links)
  - [LocalRadio](#localradio)
  - [Dependency injection](#dependency-injection)
  - [Direct channels](#direct-channels)
  - [Field copying](#field-copying)
  - [Native AOT](#native-aot)
- [Behavior](#behavior)
- [Supported declarations](#supported-declarations)
- [Generator options](#generator-options)
- [Diagnostics](#diagnostics)
- [Build and test](#build-and-test)
- [Benchmark](#benchmark)



## Requirements

- **.NET 10 or later**: the package contains a `net10.0` library.
- **A Roslyn 5.0 or later compiler**: the .NET 10 SDK (10.0.100 or later) or Visual Studio 2026 (18.0 or later). The source generator is built against Roslyn 5.0. An older compiler skips it with warning CS9057; the build still succeeds, but using a service then throws `InvalidOperationException` at runtime because no service was registered.
- Dependencies are installed automatically: Arc.Collections, FastExpressionCompiler, and Microsoft.Extensions.DependencyInjection.Abstractions.



## Installation

Install **CrossChannel** using Package Manager Console. The package includes the source generator; no separate generator package is needed.

```
Install-Package Arc.CrossChannel
```

Or using the .NET CLI.

```
dotnet add package Arc.CrossChannel
```

**Upgrading from 2.x**: several public APIs were renamed (for example `RadioClass` → `LocalRadio`, `GhostCopy` → `FieldCopier`, `TryGetSingleResult` → `TryGetFirst`). See [CrossChannel Name Changes](doc/CrossChannel%20Name%20Changes.md), and rebuild so that the generated code is regenerated.



## Quick Start

**CrossChannel** implements the publish–subscribe pattern with the following elements.

1. **Service interface**: an interface with the `[RadioService]` attribute that derives from `IRadioService`. It is shared by the senders and the receivers.

2. **Receiver (subscriber)**: an instance which implements the interface. `Open` subscribes it to the channel of the interface and returns a link.

3. **Sender (publisher)**: calls the interface methods on the broker returned by `Send`. Each call is forwarded to every receiver; methods with a return value collect one value per receiver.

4. **Unsubscribe**: dispose the link (or call `Close`).

The source generator creates a broker for each service interface and registers it automatically (with a module initializer), so no setup code is needed.

First, define the service interface, then the receiver which implements it.

```csharp
using CrossChannel;

// First, define a common interface to be used by both the receiver and the sender.
[RadioService] // Add the RadioService attribute.
public interface IMessageService : IRadioService
{// The service interface must derive from IRadioService.
    void Message(string message);
}

public class MessageService : IMessageService
{// The receiver implements the interface.
    private readonly string prefix;

    public MessageService(string prefix)
        => this.prefix = prefix;

    public void Message(string message)
        => Console.WriteLine(this.prefix + message);
}
```

Then subscribe and send (for example, in `Program.cs`).

```csharp
using CrossChannel;

// Open() subscribes the receiver and returns a link; disposing the link unsubscribes it.
using (var link = Radio.Open<IMessageService>(new MessageService("Test: ")))
{
    // Send a message to every receiver. Output: "Test: message"
    Radio.Send<IMessageService>().Message("message");
}

// Not displayed: the receiver has unsubscribed.
Radio.Send<IMessageService>().Message("message not received");

// LocalRadio has its own, independent set of channels.
var radio = new LocalRadio();
using (radio.Open<IMessageService>(new MessageService("Local: ")))
{
    // Output: "Local: message"
    radio.Send<IMessageService>().Message("message");
}
```

Dispose every link you no longer need: the radio keeps the receiver of an undisposed link alive and subscribed (a memory leak), unless the link was opened with a [weak reference](#weak-references).

[QuickStart/Example.cs](QuickStart/Example.cs) is a runnable version.



## Cheat sheet

```csharp
[RadioService] // The RadioService attribute is required.
public interface ITestService : IRadioService // The service interface must derive from IRadioService.
{// The return type of each method must be void, Task, RadioResult<T>, or Task<RadioResult<T>>.

    void Test1(); // A method without a return value.

    RadioResult<int> Test2(int x); // With a return value. The number of values depends on the number of receivers, so they are wrapped in RadioResult<T>.

    Task Test3(); // An asynchronous method without a return value.

    Task<RadioResult<int>> Test4(); // An asynchronous method with a return value.
}

public class TestService : ITestService
{
    void ITestService.Test1()
    {// Receivers may be called from any thread, and from several threads at once, so make them thread-safe.
    }

    RadioResult<int> ITestService.Test2(int x)
    {// Wrap the return value in RadioResult<T> (return default to contribute no value).
        return new(x * 2);
    }

    Task ITestService.Test3()
    {// May be called on any thread (UI or non-UI).
        return Task.CompletedTask;
    }

    async Task<RadioResult<int>> ITestService.Test4()
    {
        await Task.Delay(10);
        return new(1);
    }
}
```

```csharp
var radio = new LocalRadio(); // Or use the static Radio, which has the same methods.

var link = radio.Open<ITestService>(new TestService()); // Subscribe. Returns null if the channel is full (MaxLinks).
using var keyedLink = radio.OpenWithKey<ITestService, int>(new TestService(), 1); // Subscribe to the channel of key 1.
var receiver = new TestService(); // The caller must keep the receiver alive: a weak link ends once it is collected.
radio.Open<ITestService>(receiver, useWeakReference: true); // Subscribe with a weak reference.

radio.Send<ITestService>().Test1(); // Publish to the receivers without a key.
radio.SendWithKey<ITestService, int>(1).Test1(); // Publish to the receivers of key 1 only.
RadioResult<int> results = radio.Send<ITestService>().Test2(1); // One value per receiver.
await radio.Send<ITestService>().Test3(); // Completes once every receiver's task has completed.
var asyncResults = await radio.Send<ITestService>().Test4();

var count = radio.GetChannel<ITestService>().Count; // The number of links (the keyed link is in another channel).
link?.Dispose(); // Unsubscribe (Close() does the same). Both can be called repeatedly.
var isOpen = link?.IsOpen; // false
```



## Features

### Return values

A receiver returns a single value, but a sender collects one value per receiver, so the results are wrapped in `RadioResult<T>`. Receivers which return an empty result are skipped.

```csharp
[RadioService]
public interface ICalcService : IRadioService
{
    RadioResult<int> Double(int x);
}

public class CalcService : ICalcService
{
    public RadioResult<int> Double(int x)
        => new(x * 2); // Each receiver returns one value.
}
```

```csharp
var radio = new LocalRadio();
using (radio.Open<ICalcService>(new CalcService()))
using (radio.Open<ICalcService>(new CalcService()))
{
    var result = radio.Send<ICalcService>().Double(2);

    var count = result.Count; // 2
    var isEmpty = result.IsEmpty; // false
    var retrieved = result.TryGetFirst(out var value); // true, and value is 4.
    foreach (var x in result) { } // Enumerates every value.
    var text = result.ToString(); // "[4, 4]"
}

// With no receiver, the result is empty.
var empty = radio.Send<ICalcService>().Double(2).IsEmpty; // true
```

On the receiving side, return `new(value)` for a value, or `default` (`RadioResult<T>.Empty`) to contribute nothing. The constructor also has an array overload (`T[]`), so use `RadioResult<T>.FromValue(value)`, which always gives a single result, in these cases:

- The value is `null`, `default`, or `[]`: the call is either ambiguous (a compile error) or binds to the array overload.
- `T` is a type that arrays convert to, such as `object`, `Array`, `IEnumerable`, or `IEnumerable<object>`: an array whose elements convert to `T` by reference silently binds to the array overload. For example, with `T` = `object`, `new(new[] { "a", "b" })` gives two results (`int[]` stays a single value), `new([])` gives an empty result, and `new(null)` throws `ArgumentNullException`.

When `T` is itself an array type (for example `byte[]`), `new(array)` already gives a single result.

`FromArray` and the array constructor turn an empty array into an empty result and keep arrays with two or more elements without copying; do not modify such an array afterwards, especially while the result is used as a dictionary key. Equality (`Equals`, `==`, `!=`) compares the values in order, and `GetHashCode` is consistent with it. `foreach` on a `RadioResult<T>` uses a struct enumerator; enumerating through `IEnumerable<T>` (for example with LINQ) boxes it.

Receivers should return zero or one value. Synchronous delivery and multi-receiver asynchronous aggregation take only the first value of each receiver; a single asynchronous receiver's task is passed through with its entire result.



### Asynchronous methods

`Task` and `Task<RadioResult<T>>` are supported. `ValueTask`, `ValueTask<T>`, and `Task<T>` with a `T` other than `RadioResult<…>` are rejected (CCG003).

```csharp
[RadioService]
public interface IAsyncService : IRadioService
{
    Task Save();

    Task<RadioResult<int>> Load();
}

public class AsyncService : IAsyncService
{
    public async Task Save()
        => await Task.Delay(10);

    public async Task<RadioResult<int>> Load()
    {
        await Task.Delay(10);
        return new(1);
    }
}
```

```csharp
var radio = new LocalRadio();
using var link1 = radio.Open<IAsyncService>(new AsyncService());
using var link2 = radio.Open<IAsyncService>(new AsyncService());

await radio.Send<IAsyncService>().Save(); // Completes once every receiver's task has completed.
var results = await radio.Send<IAsyncService>().Load(); // One value per responding receiver: [1, 1]
```

The broker invokes the receivers one after another without awaiting them. Each receiver runs synchronously until its first incomplete `await`, then the next receiver is invoked, so their asynchronous parts overlap. The returned task completes once every receiver task has completed:

- With no receiver, it is a cached, already completed task. For `Task`, receiver tasks which already completed successfully are skipped, so this also applies when every receiver task has already completed successfully.
- When only one receiver task remains, that task is returned as is, including the entire result.
- For `Task<RadioResult<T>>` with several receivers, the result is built synchronously when every receiver task has already completed successfully (no `Task.WhenAll`, no async state machine).

Exceptions and cancellation:

- `Task`: the broker returns `Task.WhenAll` of the remaining receiver tasks; it holds the exceptions of every faulted receiver in completion order, or is canceled if none faulted but one was canceled.
- `Task<RadioResult<T>>`: the returned task holds the exceptions of every faulted receiver in receiver order; if none faulted but one was canceled, it is canceled.
- `await` rethrows only the first exception; read `task.Exception.InnerExceptions` for all of them.
- A receiver which throws before returning its task stops the delivery: the remaining receivers are not invoked, the exception is returned in a faulted task, and tasks already started are not awaited.

Nullable return types (`Task?`, `Task<RadioResult<T>>?`) are supported. A receiver which returns `null` counts as completed with no result; the broker itself never returns `null`.



### Weak references

Open a link with `useWeakReference: true` to let the receiver be garbage collected without disposing its link. Something else, such as the owner of a view, must keep the receiver alive while it should receive messages. Collection timing is nondeterministic.

```csharp
public sealed class MessageView : IMessageService
{// The owner of the view (e.g. a window) keeps it alive; the radio does not.
    public MessageView()
        => Radio.Open<IMessageService>(this, useWeakReference: true); // No need to keep or dispose the link.

    public void Message(string message)
        => Console.WriteLine("View: " + message);
}
```

A link to a collected receiver is removed when a send encounters it, during periodic cleanup, or before a full channel rejects a new subscription; `Count` may include it until then. `link.TryGetInstance(out var instance)` returns `false` once the receiver is collected, and `link.IsOpen` becomes `false` once the link is removed. A weak link can still be disposed explicitly.



### Keyed channels

```csharp
// Subscribe to the channel of key 1.
using (Radio.OpenWithKey<IMessageService, int>(new MessageService("Key: "), 1))
{
    Radio.SendWithKey<IMessageService, int>(0).Message("0"); // Not received (no channel for key 0).
    Radio.SendWithKey<IMessageService, int>(1).Message("1"); // "Key: 1"
    Radio.SendWithKey<IMessageService, long>(1L).Message("1L"); // Not received: the key type differs.
    Radio.Send<IMessageService>().Message("no key"); // Not received: keyed and non-keyed channels are independent.
}
```

- Each key has its own channel. It is created by the first `OpenWithKey` and discarded once its last link is closed, so keys which come and go (a connection id, for example) do not accumulate.
- Keyed and non-keyed channels are independent: `Send` does not reach receivers opened with `OpenWithKey`, and `SendWithKey` does not reach receivers opened with `Open`.
- The key type is part of the lookup: `1`, `1L`, and `"1"` address different channels.
- `SendWithKey` for a key without a channel returns a shared broker which does nothing (results are empty).
- Keys must be non-null (`OpenWithKey` throws `ArgumentNullException` for a null instance or key) and keep stable `Equals`/`GetHashCode` while subscribed.
- `TryGetChannelWithKey<TService, TKey>(key, out channel)` and `TryGetChannelWithKey<TKey>(serviceType, key, out channel)` find an existing keyed channel; the latter throws `ArgumentNullException` for a null `serviceType`. Subscribe only through `OpenWithKey`: a channel found this way may already be discarded, and links opened on it directly are then unreachable by `SendWithKey`.
- A broker returned by `SendWithKey` stays bound to its channel. Once that channel is discarded, call `SendWithKey` again to reach a new channel for the same key.



### Maximum number of links

`MaxLinks` limits how many links one channel can hold; each key has its own count. When the limit is reached, `Open` first removes links to collected receivers, then returns `null` if the channel is still full. A nonpositive limit accepts no subscriptions. The default is `int.MaxValue`.

```csharp
[RadioService(MaxLinks = 1)]
public interface ISingleService : IRadioService
{
    void Message(string message);
}

public class SingleService : ISingleService
{
    public void Message(string message)
        => Console.WriteLine(message);
}
```

```csharp
var radio = new LocalRadio();
using var link = radio.Open<ISingleService>(new SingleService()); // A link.
var link2 = radio.Open<ISingleService>(new SingleService()); // null: the channel is full.
```



### LocalRadio

The static `Radio` is the fastest, but its channels are shared by the whole process. Create a `LocalRadio` when independent sets of channels are needed (per window, per test, per tenant). It has the same methods as `Radio`.

```csharp
var radio = new LocalRadio();
using (radio.Open<IMessageService>(new MessageService("Local: ")))
{
    radio.Send<IMessageService>().Message("message"); // Only the receivers of this radio get it.
}
```



### Dependency injection

`AddCrossChannel` registers the radio services in an `IServiceCollection`. Building the provider needs the `Microsoft.Extensions.DependencyInjection` package; CrossChannel itself references only the abstractions.

```csharp
using CrossChannel;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCrossChannel(); // Pass false to use the static Radio instead of a LocalRadio singleton.
var provider = services.BuildServiceProvider();

// IChannel<TService>: the subscribing side.
var channel = provider.GetRequiredService<IChannel<ITestService>>();
using var link = channel.Open(new TestService());

// ISender<TService>: the sending side.
var sender = provider.GetRequiredService<ISender<ITestService>>();
sender.Send().Test1();
sender.SendWithKey(1).Test1(); // The channel of key 1.

// The service interface itself resolves to the broker, so a class can simply depend on ITestService.
var testService = provider.GetRequiredService<ITestService>();
testService.Test1();
```

- Every registration is a singleton. `IChannel<TService>` is registered for each service; the service interface (resolved to the broker) and `ISender<TService>` are registered as well, unless the service opts out (see below).
- With `useLocalRadio: true` (the default), everything goes through a `LocalRadio` singleton, which can also be resolved; each service provider gets its own. With `false`, everything goes through the static `Radio`.
- Existing registrations are kept (`TryAdd`): a `LocalRadio` registered earlier is used, and an implementation of a service interface registered earlier takes precedence over the broker. Calling `AddCrossChannel` again adds only the missing services. The first call decides the radio; mixing `true` and `false` is not supported.
- Only the services registered in `RadioServiceRegistry` at the time of the call are added. A library's services are registered when the library is first used; if `AddCrossChannel` may run earlier, call `RuntimeHelpers.RunModuleConstructor(typeof(ILibraryService).Module.ModuleHandle)` first.
- The container disposes the singletons it created. For a service which derives from `IDisposable`, that includes the broker once it has been resolved, and disposing the broker calls `Dispose` on every receiver; opt such services out of the broker registration.

```csharp
[RadioService(AutoRegisterServiceAndSender = false)] // Only IChannel<IManualService> is registered.
public interface IManualService : IRadioService
{
    void Message(string message);
}
```



### Direct channels

Use `new Channel<TService>()` for a standalone channel which is independent of `Radio` and `LocalRadio`. `Channel<TService>` implements `IChannel<TService>`.

```csharp
var channel = new Channel<IMessageService>(); // Independent of Radio and LocalRadio.
using var link = channel.Open(new MessageService("Direct: "));
channel.GetBroker().Message("message"); // "Direct: message"
```

`Count` reports the registered links and `MaxLinks` comes from the service attribute. Dispose each returned link to unsubscribe; `Close` is equivalent and both can be called repeatedly.

Other APIs for advanced use:

- `GetChannel(Type)` on `Radio` and `LocalRadio` returns the same channel as `GetChannel<TService>()` as a non-generic `Channel`, whose `GetBroker()` returns `object`.
- `RadioServiceRegistry` lists the registered services (`Registrations`, `GetRegistration<TService>()`, `GetRegistration(Type)`). Each `RadioServiceRegistration` exposes the generated factories and the attribute options.
- `RadioServiceRegistry.GetEmptyChannel<TService>()` returns the shared channel which accepts no subscriptions; its broker is the one `SendWithKey` returns for a missing key.
- `Channel<TService>.DangerousGetLinks()` and `RadioTask` exist for the generated code and are hidden from IntelliSense.



### Field copying

`FieldCopier.Copy<T>(ref source, ref destination)` copies the instance fields declared by `T` and its base types, including private, readonly, and auto-property backing fields.

```csharp
public class Settings
{
    private readonly int level;

    public Settings(string name, int level)
    {
        this.Name = name;
        this.level = level;
    }

    public string Name { get; } // Get-only: its readonly backing field is copied as well.

    public int Level => this.level;
}
```

```csharp
var source = new Settings("A", 1);
var destination = new Settings("B", 2);
FieldCopier.Copy(ref source, ref destination); // destination: Name "A", Level 1
```

- The copy is shallow: reference fields keep referring to the same objects. Properties are not invoked, and fields declared only by a runtime subtype of `T` are not copied. Arrays have no instance fields, so their elements are not copied.
- Pass existing, non-null instances. `string` is not supported (its length is part of its layout) and throws `NotSupportedException`.
- `FieldCopier.GetDelegate<T>()` returns the cached `CopyDelegate<T>` which `Copy<T>` uses.
- When dynamic code is available, the delegate is compiled once per type and copies without allocating. Readonly pointer and function pointer fields are the exception: they are copied through reflection inside the delegate, which boxes them on each copy.
- Under Native AOT, a reflection-based delegate is used instead; it may box value-type fields.
- `T` is annotated with `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllFields)]` for trimming. Generic wrappers around these methods must propagate the annotation to their type parameter.



### Native AOT

CrossChannel is compatible with **Native AOT** and trimming. The library is built with `IsAotCompatible` (trimming and AOT analyzers enabled) and produces no trimming or AOT warnings. The delivery code is emitted by the source generator, not by reflection or `Reflection.Emit`, so everything works unchanged, including keyed channels, `ISender<TService>`, and the `AddCrossChannel` registrations.

Set `<PublishAot>true</PublishAot>` in the application's project file and publish for a runtime identifier:

```
dotnet publish -c Release -r linux-x64
```

`FieldCopier` uses its reflection-based delegate under Native AOT (see [Field copying](#field-copying)).



## Behavior

- **Registration**: the generated module initializer registers the services of an assembly when the assembly is first used (for the application, before `Main`). Using an unregistered service through `GetChannel`, `Open`, `OpenWithKey`, `Send`, `SendWithKey`, `new Channel<TService>()`, `GetRegistration`, or `GetEmptyChannel` throws `InvalidOperationException`; `TryGetChannelWithKey` returns `false`. Failed lookups are not cached, so a service registered later still works.
- **No receiver**: sending does nothing. `RadioResult<T>` is empty, and tasks are already completed.
- **Order**: receivers are invoked, and results collected, in the internal link order of the channel, which is not necessarily the subscription order. Do not rely on a specific order.
- **Exceptions**: a synchronous receiver exception stops further invocations. `void` and `RadioResult<T>` methods propagate it immediately; task-returning methods return it in a faulted task. See [Asynchronous methods](#asynchronous-methods) for faulted and canceled receiver tasks.
- **Thread safety**: sending to a channel takes no lock, except to remove links to collected receivers. Subscription changes take the channel lock; keyed lookups and keyed subscription changes take the lock of the service/key-type map. Sending is not a snapshot: concurrent subscription changes may affect a delivery in progress. Disposing a link does not wait for calls already in progress. Receivers may be called from several sending threads at once and must synchronize their own state.
- **Allocations**: after initialization, the broker allocates nothing for `void` delivery, for `Task` delivery whose receiver tasks have already completed successfully, and for `RadioResult<T>` delivery that collects zero or one value, however many receivers there are. Two or more result values need a result array; for `RadioResult<T>` delivery it is first sized to the number of links and copied to a trimmed array when fewer receivers return a value. Two or more `Task<RadioResult<T>>` receivers need a task array and, unless every result is empty, a result task. Waiting for pending receiver tasks may allocate as well (`Task.WhenAll` or an aggregation task). Allocations made by the receivers are separate.



## Supported declarations

- **Service interface**: an interface with `[RadioService]` that derives from `IRadioService`, directly or through a base interface. It may be `partial`, and it may be nested in classes, structs, or interfaces; every containing type must be `partial` (CCG001). Generic interfaces and generic containing types (CCG004), and file-local (`file`) interfaces and containing types (CCG005), are not supported.
- **Return types**: `void`, `Task`, `RadioResult<T>`, `Task<RadioResult<T>>`, and the nullable `Task?` and `Task<RadioResult<T>>?`. `T` may be any type argument except `dynamic`, including delegates (`RadioResult<Action>`), tuples, arrays, and nullable reference types. Any other return type, such as `ValueTask`, `Task<int>`, a delegate, or `dynamic`, reports CCG003.
- **Parameters**: by-value parameters, including `dynamic`, nullable, and (in unsafe code) pointer and function pointer ones. `ref`, `in`, `out`, and `ref readonly` parameters report CCG004.
- **Members**: the broker implements every abstract or virtual (default) instance method of the service and of its base interfaces, including methods inherited from `System` interfaces such as `IProgress<T>` and `IDisposable`. It skips private, sealed, and non-abstract static methods, and protected or private protected methods with a body. An explicit implementation of a base interface method in the service is skipped too, but the base method itself is brokered. Properties, indexers, events, generic methods, `ref` returns, static abstract members, and protected or private protected abstract methods report CCG004.
- **Names**: the generated code qualifies every name with `global::` and keeps nullable annotations, so it works next to user types or namespaces named `System`, `CrossChannel`, `Channel`, and so on. Escaped keywords (`@event`), namespaces which differ only in case, and attribute aliases are supported.
- **Obsolete**: services, methods, and types marked `[Obsolete]` (with any `DiagnosticId`, even with `error: true`) produce no diagnostics in the generated code.
- **Generated members**: the generated root class and its module initializer are `internal`. For a nested service, the generator adds an `internal` `__Initialize__` method and a private broker class to the containing type.



## Generator options

Place `[CrossChannelGeneratorOptions]` on any interface of the project to configure the generator. Only the first one found takes effect.

```csharp
[CrossChannelGeneratorOptions(GenerateToFile = true)]
internal interface IGeneratorOptions
{
}
```

- `GenerateToFile = true` writes `gen.CrossChannel.<namespace>.cs` files to a `Generated` folder beside the annotated file instead of adding the source to the compilation in memory. If the folder does not exist, the source is added in memory. Written files are compiled only by the next build, and excluding them (for example with `<Compile Remove="Generated\*" />`) leaves the services unregistered. To inspect the output without changing compilation, prefer the standard `EmitCompilerGeneratedFiles` property.
- `AttachDebugger` is reserved and has no effect.



## Diagnostics

All diagnostics are errors.

| Id     | Description                                                  |
| ------ | ------------------------------------------------------------ |
| CCG001 | A type containing a service interface is not `partial`.     |
| CCG002 | An interface with the `RadioService` attribute does not derive from `IRadioService`. |
| CCG003 | A method's return type is not `void`, `Task`, `RadioResult<T>`, or `Task<RadioResult<T>>` (nullable `Task?` and `Task<RadioResult<T>>?` are allowed), or `T` is `dynamic`. |
| CCG004 | Unsupported declaration: a generic service interface or containing type; a property, indexer, or event; a generic method; a static abstract member; a protected or private protected abstract method; a `ref` return; or a `ref`, `in`, `out`, or `ref readonly` parameter. |
| CCG005 | A service interface, or a type containing it, is file-local (`file`). |



## Build and test

```sh
dotnet build CrossChannel.slnx -c Release -warnaserror
dotnet test --solution CrossChannel.slnx -c Release --no-build
```

`global.json` selects the Microsoft.Testing.Platform runner, so `dotnet test` takes `--solution` or `--project` (.NET 10 SDK or later). `XUnitTest` covers delivery, subscription lifetimes, keyed channels, concurrency, DI, and field copying. `GeneratorTest` runs the generator with Roslyn, checks the diagnostics for invalid declarations, and checks that the generated code compiles without warnings.

The test projects are executables, so collect coverage by running them under `dotnet-coverage` after building:

```sh
dotnet tool install --global dotnet-coverage
dotnet-coverage collect -f cobertura -o TestResults/runtime.cobertura.xml dotnet XUnitTest/bin/Release/net10.0/XUnitTest.dll
dotnet-coverage collect -f cobertura -o TestResults/generator.cobertura.xml dotnet GeneratorTest/bin/Release/net10.0/GeneratorTest.dll
```

In the runtime report, read the `CrossChannel` package; in the generator report, the `CrossChannelGenerator` package. Native AOT itself is checked only by `AotTest` below.

`AotTest` is a smoke test which exercises the radios, keyed channels, DI (including struct keys and the registration opt-out), synchronous and asynchronous delivery, and field copying from a Native AOT binary. To run it on Windows (with the [Native AOT prerequisites](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/#prerequisites) installed):

```powershell
dotnet publish AotTest/AotTest.csproj -c Release -r win-x64 -warnaserror
./AotTest/bin/Release/net10.0/win-x64/publish/AotTest.exe --require-no-dynamic-code
```

`AotTest` sets `PublishAot` in its project file, so the setting does not propagate to the `netstandard2.0` source generator project. Without a native linker, the trimming and the reflection fallback can be checked on CoreCLR instead (this does not replace the Native AOT run):

```powershell
dotnet publish AotTest/AotTest.csproj -c Release -r win-x64 -p:PublishAot=false -p:DynamicCodeSupport=false --self-contained true -o artifacts/trimmed-reflection -warnaserror
./artifacts/trimmed-reflection/AotTest.exe --require-no-dynamic-code
```

On every push to `main`, CI builds, runs the tests, and publishes and runs `AotTest` on Windows and Linux.



## Benchmark

Run the benchmarks from the `Benchmark` directory:

```sh
dotnet run -c Release -- --allocation-check
dotnet run -c Release -- --filter "*AllocationBenchmark*"
dotnet run -c Release -- --filter "*H2HBenchmark*"
```

- `--allocation-check` prints the bytes allocated per operation, without BenchmarkDotNet: `void`, completed `Task`, and `RadioResult<int>` delivery to 0, 1, and 8 receivers, and a sparse `Task<RadioResult<int>>` aggregation.
- Other arguments go to BenchmarkDotNet's `BenchmarkSwitcher` (`--filter`, `--list flat`, and so on); without arguments, it asks which benchmark to run. The benchmarks are `AllocationBenchmark`, `AggregateAllocationBenchmark`, `FieldCopierBenchmark`, `RadioResultTest`, `H2HBenchmark` (CrossChannel versus other Pub/Sub libraries), and `CrossChannelBenchmark`.

### Historical results

The following results were measured with earlier versions of CrossChannel and are kept for reference only. They are not measurements of the current implementation; runtime, hardware, and package versions affect the results, so re-run the benchmarks before comparing.

**Comparison with other Pub/Sub libraries** ([H2HBenchmark](/Benchmark/Benchmarks/H2HBenchmark.cs)): each benchmark opens a link (subscribe), sends messages (publish), and disposes the link (unsubscribe). `OpenSend8` sends eight messages to one receiver; `OpenSend88` sends eight messages to eight receivers.

CC: [archi-Doc/CrossChannel](https://github.com/archi-Doc/CrossChannel) (static `Radio`)

CC2: [archi-Doc/CrossChannel](https://github.com/archi-Doc/CrossChannel) (`LocalRadio`, slightly slower than the static `Radio`, but still very fast)

MP: [Cysharp/MessagePipe](https://github.com/Cysharp/MessagePipe) (these rows also include resolving the publisher and subscriber from DI on each operation; the current benchmark resolves them once)

PS: [upta/pubsub](https://github.com/upta/pubsub)

| Method         |        Mean |     Error |    StdDev |      Median |   Gen0 | Allocated |
| -------------- | ----------: | --------: | --------: | ----------: | -----: | --------: |
| CC_OpenSend    |    29.57 ns |  0.502 ns |  0.751 ns |    29.21 ns | 0.0025 |      48 B |
| CC_OpenSend8   |    34.50 ns |  0.876 ns |  1.283 ns |    34.24 ns | 0.0025 |      48 B |
| CC_OpenSend88  |   279.59 ns |  3.614 ns |  5.184 ns |   281.12 ns | 0.0200 |     384 B |
| CC2_OpenSend   |    29.70 ns |  0.203 ns |  0.304 ns |    29.68 ns | 0.0025 |      48 B |
| CC2_OpenSend8  |    56.28 ns |  0.227 ns |  0.333 ns |    56.21 ns | 0.0025 |      48 B |
| CC2_OpenSend88 |   346.41 ns |  1.120 ns |  1.676 ns |   346.32 ns | 0.0200 |     384 B |
| MP_OpenSend    |    65.33 ns |  0.121 ns |  0.174 ns |    65.34 ns | 0.0029 |      56 B |
| MP_OpenSend8   |    67.14 ns |  0.125 ns |  0.175 ns |    67.13 ns | 0.0029 |      56 B |
| MP_OpenSend88  |   595.81 ns | 14.852 ns | 22.230 ns |   596.29 ns | 0.0229 |     448 B |
| PS_OpenSend    |   154.14 ns |  3.246 ns |  4.859 ns |   153.97 ns | 0.0229 |     432 B |
| PS_OpenSend8   |   382.60 ns | 14.223 ns | 21.288 ns |   369.83 ns | 0.0734 |    1384 B |
| PS_OpenSend88  | 2,756.65 ns | 49.612 ns | 72.721 ns | 2,788.00 ns | 0.2060 |    3904 B |

**CrossChannel operations** ([CrossChannelBenchmark](/Benchmark/Benchmarks/CrossChannelBenchmark.cs)): the `Class_` rows use `LocalRadio` (formerly `RadioClass`). The `Class_` and `OpenSend8_Key` benchmarks are currently commented out in the benchmark code.

- `Radio` is the fastest since it caches channels in static generic types.
- `LocalRadio` looks channels up in a lock-free type-keyed hashtable, which is a bit slower than static caching, but still fast enough.
- Keyed channels are slightly slower.
- Weak subscriptions allocate a weak-reference object in addition to their link.

| Method               |       Mean |      Error |     StdDev |   Gen0 | Allocated |
| -------------------- | ---------: | ---------: | ---------: | -----: | --------: |
| Send                 |   1.916 ns |  0.0200 ns |  0.0287 ns |      - |         - |
| OpenSend             |  39.654 ns |  0.3066 ns |  0.4494 ns | 0.0038 |      48 B |
| OpenSend8            |  54.575 ns |  0.3954 ns |  0.5796 ns | 0.0038 |      48 B |
| OpenSend_Weak        | 134.302 ns |  7.7571 ns | 11.3703 ns | 0.0057 |      72 B |
| OpenSend8_Weak       | 139.289 ns |  3.1632 ns |  4.5366 ns | 0.0057 |      72 B |
| SendKey              |   8.722 ns |  0.1016 ns |  0.1520 ns |      - |         - |
| OpenSend_Key         | 124.375 ns |  4.7073 ns |  6.5990 ns | 0.0241 |     304 B |
| OpenSend8_Key        | 287.545 ns |  9.2775 ns | 13.8862 ns | 0.0238 |     304 B |
| Class_Send           |   8.061 ns |  0.4541 ns |  0.6656 ns |      - |         - |
| Class_OpenSend       |  47.849 ns |  2.0198 ns |  2.9606 ns | 0.0038 |      48 B |
| Class_OpenSend8      |  82.368 ns |  0.6213 ns |  0.8911 ns | 0.0038 |      48 B |
| Class_OpenSend_Weak  | 156.877 ns |  8.0446 ns | 11.5373 ns | 0.0057 |      72 B |
| Class_OpenSend8_Weak | 217.078 ns | 17.0128 ns | 23.8496 ns | 0.0057 |      72 B |
| Class_SendKey        |   9.470 ns |  0.2608 ns |  0.3823 ns |      - |         - |
| Class_OpenSend_Key   | 126.246 ns |  2.0165 ns |  2.8920 ns | 0.0241 |     304 B |
| Class_OpenSend8_Key  | 285.156 ns |  8.0497 ns | 11.5447 ns | 0.0238 |     304 B |



```csharp
ulong hkr = 3055952910;
while (true)
{
    var r = CrossChannel.Radio.Send<ITaichi>().Message(hkr++, "生きている人、いますか？");
    if (r.TryGetFirst(out _)) break;
}
```
