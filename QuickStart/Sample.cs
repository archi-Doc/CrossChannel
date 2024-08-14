using System;
using System.Threading.Tasks;
using CrossChannel;

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
    {
        this.prefix = prefix;
    }

    public void Message(string message)
    {
        Console.WriteLine(this.prefix + message);
    }
}

internal static class Sample
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
    }

    public static async Task Other()
    {
        // Open a channel with the key which limits the delivery of messages.
        using (var channelKey = ObsoleteRadio.OpenKey<int, string>(1, x => Console.WriteLine(x)))
        {// Channel with Key 1
            ObsoleteRadio.SendKey(0, "Key 0"); // Message is not received.
            ObsoleteRadio.SendKey(1, "Key 1"); // Message is received.
        }

        Console.WriteLine();

        // Open a two-way (bidirectional) channel which receives a message and sends back a result.
        using (var channelTwoWay = ObsoleteRadio.OpenTwoWay<int, int>(x =>
        {
            Console.WriteLine($"TwoWay: {x} -> {x * 2}");
            return x * 2;
        }))
        {
            ObsoleteRadio.SendTwoWay<int, int>(2); // TwoWay: 2 -> 4

            using (var channelTwoWay2 = ObsoleteRadio.OpenTwoWay<int, int>(x => x * 3))
            {
                // The result is an array of TResult.
                var result = ObsoleteRadio.SendTwoWay<int, int>(3); // TwoWay: 3 -> 6
                Console.WriteLine($"Results: {string.Join(", ", result)}"); // Results: 6, 9
            }
        }

        Console.WriteLine();

        // Open a channel which receives a message asynchronously.
        using (var channelAsync = ObsoleteRadio.OpenAsync<string>(async x =>
        {
            Console.WriteLine($"Received: {x}");
            await Task.Delay(1000);
            Console.WriteLine($"Done.");
        }))
        {
            await ObsoleteRadio.SendAsync("Test async");
        }
    }
}
