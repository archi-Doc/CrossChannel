## CrossChannel is Pub/Sub library for .NET



- Faster than most Pub/Sub libraries.
- Easy to use.
- Supports **Weak references**, **Asynchronous** methods.
- Supports `Action<TMessage>`, `Func<TMessage, TResult>` delegates.
- **Key** feature can limit the delivery of messages.



## Table of Contents

- [Quick Start](#quick-start)
- [Performance](#performance)



## Quick Start

```csharp
using (var c = CrossChannel.Radio.Open<int>(x => { }))
{
    CrossChannel.Radio.Send<int>(1);
}
```



## Performance

Performance is the top priority.



## Weak reference





## Close channel





## Delegates

The delegates passed to `Open()` functions must satisfy these requirements.

- **Thread safe**.
- May be called from any thread (**UI** or **non-UI**).
- Avoid recursive call (e.g. `CrossChannel.Open<int>(null, x => { CrossChannel.Send<int>(0); });`).



## Other





> CrossChannel.Radio.Send(new Taichi(3055952910, "生きている人、いますか？"));

