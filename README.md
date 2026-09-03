## **Interface-based**, **fast**, and most advanced Pub/Sub library

![Nuget](https://img.shields.io/nuget/v/Arc.CrossChannel) ![Build and Test](https://github.com/archi-Doc/CrossChannel/workflows/Build%20and%20Test/badge.svg)

- Messages are plain **interface methods**, so the compiler checks every call.
- A **source generator** emits the delivery code, so sending involves no reflection and no delegate allocation.
- Supports **return values**, **asynchronous** methods, and **weak references**.
- **Key** feature can limit the delivery of messages.
- Thread-safe.



## Table of Contents

- [Quick Start](#quick-start)
- [Performance](#performance)
- [Cheat sheet](#cheat-sheet)
- [Features](#features)
  - [Return values](#return-values)
  - [Asynchronous methods](#asynchronous-methods)
  - [Weak reference](#weak-reference)
  - [Key](#key)
  - [Maximum number of links](#maximum-number-of-links)
  - [Local radio](#local-radio)
  - [Dependency injection](#dependency-injection)
  - [Native AOT](#native-aot)
- [Behavior](#behavior)
- [Diagnostics](#diagnostics)
- [Benchmark](#benchmark)



## Quick Start

Install **CrossChannel** using Package Manager Console.

```
Install-Package Arc.CrossChannel
```

Or using the .NET CLI.

```
dotnet add package Arc.CrossChannel
```



**CrossChannel** is a library for Publish–subscribe pattern, and it consists of the following elements.

1. **Service interface**: A common interface to be used by both the subscriber and the publisher.

2. **Subscriber (receiver)**: Responsible for executing the methods of the interface. You can register the Subscriber by opening a channel.

3. **Publisher (sender)**: Call the interface methods to the Subscriber. The number of return values varies depending on the number of registered Subscribers.

4. **Unsubscribe**: Close the channel.

   

First, define an interface to be shared between the Publisher(Sender) and Subscriber(Receiver), then define the Subscriber responsible for processing (implementing the interface).

```csharp
// First, define a common interface to be used by both the receiver and the sender.
[RadioService] // Add the RadioService attribute.
public interface IMessageService : IRadioService
{// The target interface must derive from IRadioService.
    void Message(string message);
}

public class MessageService : IMessageService
{// Implement the interface.
    private readonly string prefix;

    public MessageService(string prefix)
        => this.prefix = prefix;

    public void Message(string message)
        => Console.WriteLine(this.prefix + message);
}
```



```csharp
// Open a channel which simply outputs the received message to the console.
// Open() returns a link; disposing it closes the channel.
using (var link = Radio.Open<IMessageService>(new MessageService("Test: ")))
{
    // Send a message. The result is "Test: message"
    Radio.Send<IMessageService>().Message("message");
}

// This message will not be displayed because the channel is closed.
Radio.Send<IMessageService>().Message("message not received");


// Test2: Open a channel which has a weak reference to the object.
OpenWithWeakReference();
static void OpenWithWeakReference()
{
    Radio.Open<IMessageService>(new MessageService("Test: "), true);
}

// Send a message. The result is "Test: weak message"
Radio.Send<IMessageService>().Message("weak message");

// The object is garbage collected.
GC.Collect();

// This message will not be displayed because the channel is automatically closed.
Radio.Send<IMessageService>().Message("message not received");


// Test 3: Don't forget to close the channel when you did not specify the weak reference, since this will cause memory leaks.
_ = Radio.Open<IMessageService>(new MessageService("Leak: "));
Radio.Send<IMessageService>().Message("message");

// Test 4: You can create a local radio class.
var radio = new RadioClass();
using (radio.Open<IMessageService>(new MessageService("Local: ")))
{
    // Send a message. The result is "Local: message"
    radio.Send<IMessageService>().Message("message");
}
```



## Performance

Performance is the top priority. This is a benchmark with other Pub/Sub libraries.

CC: [archi-Doc/CrossChannel](https://github.com/archi-Doc/CrossChannel) (Static Radio)

CC2: [archi-Doc/CrossChannel](https://github.com/archi-Doc/CrossChannel) (Non-static Radio, slightly slower than the static Radio, but still very fast.)

MP: [Cysharp/MessagePipe](https://github.com/Cysharp/MessagePipe)

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

The [benchmark code](/Benchmark/Benchmarks/H2HBenchmark.cs) is simple: open a channel (subscribe), send a message (publish), and close the channel (unsubscribe).



## Cheat sheet

```csharp
[RadioService] // RadioService attribute is required.
public interface ITestService : IRadioService // The target interface must derive from IRadioService
{// The return type of the interface function must be either void, Task, RadioResult<T>, Task<RadioResult<T>>.

    void Test1(); // A function without a return value.

    RadioResult<int> Test2(int x); // With a return value. Since the number of return values can be zero or more depending on the number of Subscribers, it is necessary to wrap them in a RadioResult structure.

    Task Test3(); // Asynchronous function without a return value.

    Task<RadioResult<int>> Test4(); // Asynchronous function with a return value.
}

```



```csharp
public class TestService : ITestService
{
    void ITestService.Test1()
    {// Since multiple threads may call it simultaneously, please make the function thread-safe.
    }

    RadioResult<int> ITestService.Test2(int x)
    {// Wrap the return value in RadioResult structure.
        return new(0);
    }

    async Task ITestService.Test3()
    {// May be called from any thread (UI or non-UI).
    }

    async Task<RadioResult<int>> ITestService.Test4()
    {// The asynchronous function returns after all Subscribers have completed their processing.
        return new(0);
    }
}
```



```csharp
var radio = new RadioClass(); // Or use the static Radio.

var link = radio.Open<ITestService>(new TestService()); // Subscribe. Returns null if the channel is full.
radio.Open<ITestService>(new TestService(), true); // Subscribe with a weak reference.
radio.OpenWithKey<ITestService, int>(new TestService(), 1); // Subscribe to the channel of key 1.

radio.Send<ITestService>().Test1(); // Publish.
radio.SendWithKey<ITestService, int>(1).Test1(); // Publish to the channel of key 1.

var count = radio.GetChannel<ITestService>().Count; // The number of subscribers.

link?.Dispose(); // Unsubscribe (Close() does the same).
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

using (radio.Open<ICalcService>(new CalcService()))
using (radio.Open<ICalcService>(new CalcService()))
{
    var result = radio.Send<ICalcService>().Double(2);

    var count = result.Count; // 2
    var isEmpty = result.IsEmpty; // false
    var retrieved = result.TryGetSingleResult(out var value); // true, and value is the first result.
    foreach (var x in result) { } // Enumerate every result.
    var text = result.ToString(); // "[4, 4]"
}

// With no subscriber, the result is empty.
var empty = radio.Send<ICalcService>().Double(2).IsEmpty; // true
```

On the receiving side, return `default` to contribute nothing, or use `RadioResult<T>.Single(value)` when the constructor overload would be ambiguous (a `null` reference, or an array type).



### Asynchronous methods

`Task` and `Task<RadioResult<T>>` are supported. The returned task completes once every receiver has completed, and the results are aggregated in the same way as the synchronous version.

```csharp
[RadioService]
public interface IAsyncService : IRadioService
{
    Task Save();

    Task<RadioResult<int>> Load();
}

await radio.Send<IAsyncService>().Save();
var results = await radio.Send<IAsyncService>().Load();
```

Receivers are invoked one after another without awaiting, so their processing overlaps. When there is no subscriber, or exactly one, no task or state machine is allocated by the delivery code.



### Weak reference

Weak reference is quite useful for WPF program (e.g. view service).

```csharp
 // Test2: Open a channel which has a weak reference to the instance.
 OpenWithWeakReference();
 static void OpenWithWeakReference()
 {
     Radio.Open<IMessageService>(new MessageService("Test: "), true);
 }

 // Send a message. The result is "Test: weak message"
 Radio.Send<IMessageService>().Message("weak message");

 // The object is garbage collected.
 GC.Collect();

 // This message will not be displayed because the channel is automatically closed.
 Radio.Send<IMessageService>().Message("message not received");
```



### Key

```csharp
// Open a channel with the key which limits the delivery of messages.
using (Radio.OpenWithKey<IMessageService, int>(new MessageService("Key: "), 1))
{// Channel with Key 1
    Radio.SendWithKey<IMessageService, int>(0).Message("0"); // Message is not received.
    Radio.SendWithKey<IMessageService, int>(1).Message("1"); // Message is received.
}
```

A keyed channel is created on the first subscription and discarded once its last link is closed, so keys which come and go (a connection id, for example) do not accumulate. The key type is part of the lookup: key `1` and key `"1"` address different channels.



### Maximum number of links

`MaxLinks` limits how many instances can subscribe to one channel. `Open` returns `null` once the limit is reached.

```csharp
[RadioService(MaxLinks = 1)]
public interface ISingleService : IRadioService
{
    void Message(string message);
}

using var link = radio.Open<ISingleService>(new SingleService()); // A valid link.
var link2 = radio.Open<ISingleService>(new SingleService()); // null: the channel is full.
```



### Local radio

The static `Radio` is the fastest, but its channels are shared by the whole process. Create a `RadioClass` when independent sets of channels are needed (per window, per test, per tenant).

```csharp
var radio = new RadioClass();
using (radio.Open<IMessageService>(new MessageService("Local: ")))
{
    radio.Send<IMessageService>().Message("message"); // Only the subscribers of this radio receive it.
}
```



### Dependency injection

Add `CrossChannel` to the `ServiceCollection`. Every radio service of the process is registered.

```csharp
var collection = new ServiceCollection();
collection.AddCrossChannel(); // Pass false to use the static Radio instead of a RadioClass singleton.
var provider = collection.BuildServiceProvider();

// IChannel<TService>: the subscribing side.
var channel = provider.GetRequiredService<IChannel<ITestService>>();
var link = channel.Open(new TestService());

// ISender<TService>: the sending side.
var sender = provider.GetRequiredService<ISender<ITestService>>();
sender.Send().Test1();
sender.SendWithKey(1).Test1();

// The service interface itself resolves to the broker, so a class can simply depend on ITestService.
var testService = provider.GetRequiredService<ITestService>();
testService.Test1();
```

`IChannel<TService>` is always registered. The service interface and `ISender<TService>` are registered as well, unless the service opts out:

```csharp
[RadioService(AutoRegisterServiceAndSender = false)]
public interface IManualService : IRadioService
{
    void Message(string message);
}
```



### Native AOT

CrossChannel is compatible with **Native AOT** and trimming. The library is built with `IsAotCompatible`, so it carries no trimming or AOT warnings, and the delivery code is emitted by the source generator rather than by reflection or `Reflection.Emit`.

```
dotnet publish -c Release -r linux-x64 -p:PublishAot=true
```

Everything works unchanged, including keyed channels, `ISender<TService>`, and the `AddCrossChannel` dependency injection registrations. `AotTest` in this repository is a smoke test which exercises all of them from a Native AOT binary.

The one API which needs dynamic code is `GhostCopy`. When the runtime supports it, the copy runs through a delegate compiled once per type; under Native AOT an equivalent reflection-based delegate is used instead, and the expression-tree path is trimmed away entirely.




## Behavior

- **Registration**: each assembly registers its services from a `[ModuleInitializer]`, so `ChannelRegistry` is already populated before any user code runs. An interface which derives from `IRadioService` but has no `RadioService` attribute is never registered, and using it throws `InvalidOperationException`.
- **No subscriber**: sending is a no-op and returns an empty `RadioResult<T>` or a completed task.
- **Order**: results are collected in the internal link order of the channel. Do not rely on a specific order.
- **Exceptions**: a `void` or `RadioResult<T>` method propagates the exception to the sender immediately, and the remaining receivers are not invoked. A `Task` or `Task<RadioResult<T>>` method returns a faulted task instead, so the exception surfaces when the sender awaits it.
- **Thread safety**: sending takes no lock; opening and closing links take a per-channel lock. A receiver may therefore be invoked from several threads at once, so make it thread-safe.
- **Interface inheritance**: a service interface may derive from other interfaces, and their methods are brokered as well.
- **Nested interfaces**: every type enclosing a service interface must be declared `partial`.



## Diagnostics

| Id     | Description                                                  |
| ------ | ------------------------------------------------------------ |
| CCG001 | A type enclosing the service interface is not a partial class/struct. |
| CCG002 | A type with the `RadioService` attribute does not derive from `IRadioService`. |
| CCG003 | The return type of a method is not `void`, `Task`, `RadioResult<T>`, or `Task<RadioResult<T>>`. |



## Benchmark

Here is a benchmark for each feature.

- `Radio` is the fastest since it uses static type caching.
- `RadioClass` uses `ThreadsafeTypeKeyHashtable` which is a bit slower than static type caching, but still fast enough.
- `Key` features cause slight performance degradation.
- Opening a channel with weak reference is about 4x slower, but sending messages is not that slow.

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
    if (r.TryGetSingleResult(out _)) break;
}
```
