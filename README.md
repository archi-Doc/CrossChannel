## **Interface-based**, **fast**, and most advanced Pub/Sub library

![Nuget](https://img.shields.io/nuget/v/Arc.CrossChannel) ![Build and Test](https://github.com/archi-Doc/CrossChannel/workflows/Build%20and%20Test/badge.svg)

- Supports **Weak references**, **Asynchronous** methods.
- **Key** feature can limit the delivery of messages.
- Thread-safe.



## Table of Contents

- [Quick Start](#quick-start)
- [Performance](#performance)
- [Interface and methods](#interface-and-methods)
- [Features](#features)
  - [Weak reference](#weak-reference)
  - [Key](#key)
- [Benchmark](#benchmark)



## Quick Start

Install **CrossChannel** using Package Manager Console.

```
Install-Package Arc.CrossChannel
```



**CrossChannel** is a library for Publish–subscribe pattern, and it consists of the following elements.

1. **Service interface**: A common interface to be used by both the subscriber and the publisher.

2. **Subscriber (receiver)**: Responsible for executing the methods of the interface. You can register the Subscriber by opening a channel.

3. **Publisher (sender)**: Call the interface methods to the Subscriber. The number of return values varies depending on the number of registered Subscribers.

4. **Unsubscribe**: Close the channel.

   

First, define an interface to be shared between the Publisher(Sender) and Subscriber(Receiver), then define the Subscriber responsible for processing (implementing the interface).

```csharp
// First, define a common interface to be used by both the receiver and the sender.
[RadioServiceInterface] // Add the RadioServiceInterface attribute.
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
using (var channel = Radio.Open<IMessageService>(new MessageService("Test: ")))
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

CC: [archi-Doc/CrossChannel](https://github.com/archi-Doc/CrossChannel)

MP: [Cysharp/MessagePipe](https://github.com/Cysharp/MessagePipe)

PS: [upta/pubsub](https://github.com/upta/pubsub)

| Method        |        Mean |     Error |     StdDev |   Gen0 | Allocated |
| ------------- | ----------: | --------: | ---------: | -----: | --------: |
| CC_OpenSend   |    41.91 ns |  0.650 ns |   0.972 ns | 0.0038 |      48 B |
| CC_OpenSend8  |    54.13 ns |  0.909 ns |   1.274 ns | 0.0038 |      48 B |
| CC_OpenSend88 |   365.16 ns |  4.738 ns |   7.091 ns | 0.0305 |     384 B |
| MP_OpenSend   |    89.58 ns |  0.938 ns |   1.404 ns | 0.0044 |      56 B |
| MP_OpenSend8  |    98.55 ns |  1.161 ns |   1.702 ns | 0.0044 |      56 B |
| MP_OpenSend88 |   805.17 ns | 12.591 ns |  18.845 ns | 0.0353 |     448 B |
| PS_OpenSend   |   267.89 ns | 13.514 ns |  20.228 ns | 0.0381 |     480 B |
| PS_OpenSend8  |   672.32 ns | 65.482 ns |  95.982 ns | 0.1268 |    1600 B |
| PS_OpenSend88 | 2,921.00 ns | 99.322 ns | 145.584 ns | 0.3586 |    4544 B |

The [benchmark code](/Benchmark/Benchmarks/H2HBenchmark.cs) is simple: open a channel (subscribe), send a message (publish), and close the channel (unsubscribe).



## Interface and methods

```csharp
[RadioServiceInterface] // RadioServiceInterface attribute is required.
public interface ITestService : IRadioService // The target interface must derive from IRadioService
{// The return type of the interface function must be either void, Task, RadioResult<T>, Task<RadioResult<T>>.

    void Test1(); // A function without a return value.

    RadioResult<int> Test2(int x); // With a return value. Since the number of return values can be zero or more depending on the number of Subscribers, it is necessary to wrap them in a RadioResult structure.

    Task Test3(); // Asynchronous function without a return value.

    Task<RadioResult<int>> Test4(); // Asynchronous function without a return value.
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



## Features

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

