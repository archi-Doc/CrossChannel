## **Interface-based**, **fast**, **easy-to-use**, and most advanced Pub/Sub library

![Nuget](https://img.shields.io/nuget/v/Arc.CrossChannel) ![Build and Test](https://github.com/archi-Doc/CrossChannel/workflows/Build%20and%20Test/badge.svg)

- Supports **Weak references**, **Asynchronous** methods.
- **Key** feature can limit the delivery of messages.
- Thread-safe.



## Table of Contents

- [Quick Start](#quick-start)
- [Performance](#performance)
- [Delegates](#delegates)
- [Features](#features)
  - [Weak reference](#weak-reference)
  - [Async](#async)
  - [Key](#key)
  - [Two way](#two-way)
- [Benchmark](#benchmark)



## Quick Start

Install **CrossChannel** using Package Manager Console.

```
Install-Package Arc.CrossChannel
```



**CrossChannel** is a library for Publish–subscribe pattern. It's very easy to use.

1. **Service interface**: A common interface to be used by both the subscriber and the publisher.

2. **Subscriber (receiver)**: Responsible for executing the methods of the interface. You can register the Subscriber by opening a channel.

3. **Publisher (sender)**: Call the interface function and delegate processing to the Subscriber. The number of return values varies depending on the number of registered Subscribers.

4. **Unsubscribe**: Close the channel.

   

This is a small sample code to use CrossChannel.

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
    {
        this.prefix = prefix;
    }

    public void Message(string message)
    {
        Console.WriteLine(this.prefix + message);
    }
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



## Delegates

The delegates passed to `Open()` functions must satisfy these requirements.

- Should be **Thread safe** (multiple threads may call `Send()`).
- May be called from any thread (**UI** or **non-UI**).
- Avoid recursive call (e.g. `Radio.Open<int>(x => { Radio.Send<int>(0); });`).



## Features

### Weak reference

If you specify a weak reference to a channel, you do not need to close the channel.

```csharp
CreateChannel();
void CreateChannel()
{
    var obj = new object(); // Target object

    // Open a channel with a weak reference.
    Radio.Open<int>(x => System.Console.WriteLine(x), obj);

    Radio.Send(1); // The result "1"
}

GC.Collect(); // The channel will be closed when the object is garbage collected.
Radio.Send(2); // The result ""

var channel = Radio.Open<int>(x => System.Console.WriteLine(x), new object());
channel.Dispose(); // Of course, you can close the channel manually.
```

It's quite useful for WPF program (e.g. view service).



### Async

```csharp
// Open a channel which receives a message asynchronously.
using (var channelAsync = Radio.OpenAsync<string>(async x =>
{
    Console.WriteLine($"Received: {x}");
    await Task.Delay(1000);
    Console.WriteLine($"Done.");
}))
{
    await Radio.SendAsync("Test async");
}
```



### Key

```csharp
// Open a channel with the key which limits the delivery of messages.
using (var channelKey = Radio.OpenKey<int, string>(1, x => Console.WriteLine(x)))
{// Channel with Key 1
    Radio.SendKey(0, "Key 0"); // Message is not received.
    Radio.SendKey(1, "Key 1"); // Message is received.
}

```



### Two way

```csharp
// Open a two-way (bidirectional) channel which receives a message and sends back a result.
using (var channelTwoWay = Radio.OpenTwoWay<int, int>(x =>
{
    Console.WriteLine($"TwoWay: {x} -> {x * 2}");
    return x * 2;
}))
{
    Radio.SendTwoWay<int, int>(2); // TwoWay: 2 -> 4

    using (var channelTwoWay2 = Radio.OpenTwoWay<int, int>(x => x * 3))
    {
        // The result is an array of TResult.
        var result = Radio.SendTwoWay<int, int>(3); // TwoWay: 3 -> 6
        Console.WriteLine($"Results: {string.Join(", ", result)}"); // Results: 6, 9
    }
}
```



## Benchmark

Here is a benchmark for each feature.

- `Radio` is the fastest since it uses static type caching.
- `RadioClass` uses `ConcurrentDictionary` which is a bit slower than static type caching, but still fast enough.
- `Async` and `Key` features cause slight performance degradation.
- Opening a channel with weak reference is about 8x slower, but sending messages is not that slow.

| Method              |       Mean |     Error |    StdDev |     Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
| ------------------- | ---------: | --------: | --------: | ---------: | -----: | ----: | ----: | --------: |
| Radio_Open          |  50.784 ns | 0.5998 ns | 0.8978 ns |  50.767 ns | 0.0114 |     - |     - |      48 B |
| Radio_OpenKey       |  57.632 ns | 0.1087 ns | 0.1627 ns |  57.634 ns | 0.0135 |     - |     - |      56 B |
| Radio_OpenTwoWay    |  50.183 ns | 0.2385 ns | 0.3420 ns |  50.077 ns | 0.0114 |     - |     - |      48 B |
| Radio_OpenTwoWayKey |  57.773 ns | 0.1298 ns | 0.1862 ns |  57.792 ns | 0.0135 |     - |     - |      56 B |
| Class_Open          |  70.498 ns | 1.7349 ns | 2.3747 ns |  72.347 ns | 0.0114 |     - |     - |      48 B |
| Class_OpenKey       |  92.001 ns | 0.8801 ns | 1.2900 ns |  91.433 ns | 0.0211 |     - |     - |      88 B |
| Class_OpenTwoWay    |  83.422 ns | 0.4797 ns | 0.6879 ns |  83.063 ns | 0.0191 |     - |     - |      80 B |
| Class_OpenTwoWayKey |  99.484 ns | 0.3034 ns | 0.4254 ns |  99.368 ns | 0.0230 |     - |     - |      96 B |
| Radio_Send          |   5.037 ns | 0.1905 ns | 0.2793 ns |   4.867 ns |      - |     - |     - |         - |
| Radio_SendKey       |  12.656 ns | 0.1736 ns | 0.2545 ns |  12.447 ns |      - |     - |     - |         - |
| Radio_SendTwoWay    |  18.325 ns | 0.2839 ns | 0.4161 ns |  18.139 ns | 0.0172 |     - |     - |      72 B |
| Radio_SendTwoWayKey |  26.264 ns | 0.0385 ns | 0.0539 ns |  26.253 ns | 0.0172 |     - |     - |      72 B |
| Class_Send          |  19.102 ns | 0.2788 ns | 0.4173 ns |  19.332 ns |      - |     - |     - |         - |
| Class_SendKey       |  41.382 ns | 0.4741 ns | 0.6799 ns |  41.231 ns | 0.0076 |     - |     - |      32 B |
| Class_SendTwoWay    |  46.961 ns | 0.0795 ns | 0.1140 ns |  46.956 ns | 0.0249 |     - |     - |     104 B |
| Class_SendTwoWayKey |  61.940 ns | 0.5976 ns | 0.7977 ns |  62.584 ns | 0.0267 |     - |     - |     112 B |
| Radio_Weak_Open     | 405.360 ns | 1.6483 ns | 2.3107 ns | 406.124 ns | 0.0458 |     - |     - |     192 B |
| Radio_Weak_Send     |  15.726 ns | 0.2326 ns | 0.3410 ns |  15.480 ns |      - |     - |     - |         - |



```csharp
ulong hkr = 3055952910;
while (true)
{
    var r = CrossChannel.Radio.Send<ITaichi>().Message(hkr++, "生きている人、いますか？");
    if (r.TryGetSingleResult(out _)) break;
}
```



