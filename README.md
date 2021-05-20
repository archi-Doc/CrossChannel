## CrossChannel is Pub/Sub library for .NET

![Nuget](https://img.shields.io/nuget/v/Arc.CrossChannel) ![Build and Test](https://github.com/archi-Doc/CrossChannel/workflows/Build%20and%20Test/badge.svg)

- Faster than most Pub/Sub libraries.
- Easy to use.
- Supports **Weak references**, **Asynchronous** methods.
- Supports `Action<TMessage>`, `Func<TMessage, TResult>` delegates.
- **Key** feature can limit the delivery of messages.



## Table of Contents

- [Quick Start](#quick-start)
- [Performance](#performance)
- [Weak reference](#weak-reference)
- [Close channel](#close-channel)
- [Delegates](#delegates)
- [Other](#other)



## Quick Start

Install CrossChannel using Package Manager Console.

```
Install-Package Arc.CrossChannel
```

This is a small sample code to use CrossChannel.

```csharp
// CrossChannel.Radio is a public static class.
// Open a channel which simply outputs the received message to the console.
using (var channel = CrossChannel.Radio.Open<string>(x => { Console.WriteLine("Test " + x); }))
{
    // Send a message. The result is "Test message."
    CrossChannel.Radio.Send<string>("message.");
}

// This message will not be displayed because the channel is closed.
CrossChannel.Radio.Send<string>("Message not received.");

// Open a channel which has a weak reference to an object.
OpenWithWeakReference();
static void OpenWithWeakReference()
{
    CrossChannel.Radio.Open<string>(x => { Console.WriteLine("Weak " + x); }, new object());
}

// Send a message. The result is "Weak message."
CrossChannel.Radio.Send<string>("message.");

// The object is garbage collected.
GC.Collect();

// This message will not be displayed because the channel is automatically closed.
CrossChannel.Radio.Send<string>("Message not received.");

// Don't forget to close a channel when you don't specify a weak reference, since this will cause memory leaks.
CrossChannel.Radio.Open<string>(x => { Console.WriteLine("Leak " + x); });

// You can create a local radio class.
var radio = new CrossChannel.RadioClass();
using (radio.Open<string>(x => { Console.WriteLine("Local " + x); }))
{
    // Send a message. The result is "Local message."
    radio.Send<string>("message.");
}
```



## Performance

Performance is the top priority.

This is a benchmark with other Pub/Sub libraries.

CC: [archi-Doc/CrossChannel](https://github.com/archi-Doc/CrossChannel)

MP: [Cysharp/MessagePipe](https://github.com/Cysharp/MessagePipe)

PS: [upta/pubsub](https://github.com/upta/pubsub)

| Method           |        Mean |     Error |     StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
| ---------------- | ----------: | --------: | ---------: | -----: | ----: | ----: | --------: |
| CC_OpenSend      |    55.48 ns |  0.152 ns |   0.213 ns | 0.0114 |     - |     - |      48 B |
| CC_OpenSend8     |    88.95 ns |  0.128 ns |   0.192 ns | 0.0114 |     - |     - |      48 B |
| MP_OpenSend      |   179.64 ns |  0.631 ns |   0.884 ns | 0.0134 |     - |     - |      56 B |
| MP_OpenSend8     |   234.70 ns |  0.394 ns |   0.590 ns | 0.0134 |     - |     - |      56 B |
| PS_OpenSend      |   402.67 ns |  0.783 ns |   1.148 ns | 0.1144 |     - |     - |     480 B |
| PS_OpenSend8     | 1,253.93 ns | 77.629 ns | 116.191 ns | 0.3815 |     - |     - |   1,600 B |
| CC_OpenSend_Key  |    71.84 ns |  0.929 ns |   1.390 ns | 0.0135 |     - |     - |      56 B |
| CC_OpenSend8_Key |   168.34 ns |  2.873 ns |   4.300 ns | 0.0134 |     - |     - |      56 B |
| MP_OpenSend_Key  |   287.17 ns |  4.814 ns |   6.904 ns | 0.0725 |     - |     - |     304 B |
| MP_OpenSend8_Key |   467.52 ns |  0.884 ns |   1.295 ns | 0.0725 |     - |     - |     304 B |

The actual code is simple: open a channel (subscribe), send a message (publish), and close the channel (unsubscribe).



## Weak reference

.



## Close channel

.



## Delegates

The delegates passed to `Open()` functions must satisfy these requirements.

- **Thread safe**.
- May be called from any thread (**UI** or **non-UI**).
- Avoid recursive call (e.g. `CrossChannel.Open<int>(null, x => { CrossChannel.Send<int>(0); });`).



## Other

.



> CrossChannel.Radio.Send(new Taichi(3055952910, "生きている人、いますか？"));

