using System;
using System.Threading.Tasks;
using CrossChannel;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously


namespace QuickStart;

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

[RadioServiceInterface] // RadioServiceInterface attribute is required.
public interface ITestService : IRadioService // The target interface must derive from IRadioService
{// The return type of the interface function must be either void, Task, RadioResult<T>, Task<RadioResult<T>>.

    void Test1(); // A function without a return value.

    RadioResult<int> Test2(int x); // With a return value. Since the number of return values can be zero or more depending on the number of Subscribers, it is necessary to wrap them in a RadioResult structure.

    Task Test3(); // Asynchronous function without a return value.

    Task<RadioResult<int>> Test4(); // Asynchronous function without a return value.
}

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

internal static class Example
{
    public static void QuickStart()
    {
        // Test 1: CrossChannel.Radio is a public static class.
        // Open a channel which simply outputs the received message to the console.
        using (var channel = Radio.Open<IMessageService>(new MessageService("Test: ")))
        {
            // Send a message. The result is "Test: message"
            Radio.Send<IMessageService>().Message("message");
        }

        // This message will not be displayed because the channel is closed.
        Radio.Send<IMessageService>().Message("message not received");


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
    }

    public static async Task Other()
    {
        // Open a channel with the key which limits the delivery of messages.
        using (Radio.OpenWithKey<IMessageService, int>(new MessageService("Key: "), 1))
        {// Channel with Key 1
            Radio.SendWithKey<IMessageService, int>(0).Message("0"); // Message is not received.
            Radio.SendWithKey<IMessageService, int>(1).Message("1"); // Message is received.
        }

        Console.WriteLine();

        using (Radio.Open<IExampleService>(new ExampleService()))
        {
            var result = Radio.Send<IExampleService>().Double(2);
            Console.WriteLine($"Double: {result.ToString()}");

            using (Radio.Open<IExampleService>(new ExampleService()))
            {
                result = Radio.Send<IExampleService>().Double(3);
                Console.WriteLine($"Double: {result.ToString()}");
            }

            await Radio.Send<IExampleService>().AsyncMethod(1000);
        }
    }
}


[RadioServiceInterface]
public interface IExampleService : IRadioService
{
    RadioResult<int> Double(int x); // Define a function with a return value. To share the return value on the receiving side as well, the type must be either void, Task, RadioResult<T>, or Task<RadioResult<T>>.

    Task AsyncMethod(int delay);
}

public class ExampleService : IExampleService
{// Implement the interface.
    public ExampleService()
    {
    }

    RadioResult<int> IExampleService.Double(int x)
        => new(x * 2);

    async Task IExampleService.AsyncMethod(int delay)
    {
        await Task.Delay(delay);

        Console.WriteLine("Start");

        await Task.Delay(delay);

        Console.WriteLine("End");
    }
}
