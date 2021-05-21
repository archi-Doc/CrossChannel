using System;
using CrossChannel;

namespace ConsoleApp1
{
    internal static class Sample
    {
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
    }
}
