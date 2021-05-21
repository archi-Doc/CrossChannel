using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("CrossChannel Sample:");
            Console.WriteLine();

            Sample.WeakReference();

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

            // Don't forget to close the channel when you did not specify a weak reference, since this will cause memory leaks.
            CrossChannel.Radio.Open<string>(x => { Console.WriteLine("Leak " + x); });

            // You can create a local radio class.
            var radio = new CrossChannel.RadioClass();
            using (radio.Open<string>(x => { Console.WriteLine("Local " + x); }))
            {
                // Send a message. The result is "Local message."
                radio.Send<string>("message.");
            }
        }
    }
}
