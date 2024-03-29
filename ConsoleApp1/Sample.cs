﻿using System;
using System.Threading.Tasks;
using CrossChannel;

namespace ConsoleApp1;

internal static class Sample
{
    public static void QuickStart()
    {
        // Test 1: CrossChannel.Radio is a public static class.
        // Open a channel which simply outputs the received message to the console.
        using (var channel = CrossChannel.Radio.Open<string>(x => { Console.WriteLine("Test " + x); }))
        {
            // Send a message. The result is "Test message."
            CrossChannel.Radio.Send<string>("message.");
        }

        // This message will not be displayed because the channel is closed.
        CrossChannel.Radio.Send<string>("Message not received.");


        // Test2: Open a channel which has a weak reference to an object.
        OpenWithWeakReference();
        static void OpenWithWeakReference()
        {
            CrossChannel.Radio.Open<string>(x => { Console.WriteLine("Weak " + x); }, new object());

            var c = new TestClass();
            var c2 = new TestClass2();
            CrossChannel.Radio.Open<string>(TestClass2.Message, c2);
        }

        // Send a message. The result is "Weak message." and "TestClass: message."
        CrossChannel.Radio.Send<string>("message.");

        // The object is garbage collected.
        GC.Collect();

        // This message will not be displayed because the channel is automatically closed.
        CrossChannel.Radio.Send<string>("Message not received.");


        // Test 3: Don't forget to close the channel when you did not specify a weak reference, since this will cause memory leaks.
        CrossChannel.Radio.Open<string>(x => { Console.WriteLine("Leak " + x); });
        CrossChannel.Radio.Send<string>("message.");


        // Test 4: You can create a local radio class.
        var radio = new CrossChannel.RadioClass();
        using (radio.Open<string>(x => { Console.WriteLine("Local " + x); }))
        {
            // Send a message. The result is "Local message."
            radio.Send<string>("message.");
        }
    }

    public static void WeakReference()
    {
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
    }

    public static async Task Other()
    {
        // Open a channel with the key which limits the delivery of messages.
        using (var channelKey = Radio.OpenKey<int, string>(1, x => Console.WriteLine(x)))
        {// Channel with Key 1
            Radio.SendKey(0, "Key 0"); // Message is not received.
            Radio.SendKey(1, "Key 1"); // Message is received.
        }

        Console.WriteLine();

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

        Console.WriteLine();

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
    }

    private class TestClass
    {
        public TestClass()
        {
            CrossChannel.Radio.Open<string>(Message, this);
        }

        public void Message(string message)
        {
            Console.WriteLine($"TestClass: {message}");
        }
    }

    private class TestClass2
    {
        public static void Message(string message)
        {
            Console.WriteLine($"TestClass2: {message}");
        }
    }
}
