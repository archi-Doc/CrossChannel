// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using CrossChannel;
using Xunit;

namespace XUnitTest;

[RadioService]
public interface IVoidService : IRadioService
{
    void NoParameter();

    void Add(int x);

    void Concat(string a, int b, double c);

    void Nullable(string? a, int? b);

    void Generic(List<int> a, KeyValuePair<string, int> b);

    void Throw();
}

public class VoidService : IVoidService
{
    public int Count { get; private set; }

    public int Sum { get; private set; }

    public string Text { get; private set; } = string.Empty;

    void IVoidService.NoParameter() => this.Count++;

    void IVoidService.Add(int x)
    {
        this.Count++;
        this.Sum += x;
    }

    void IVoidService.Concat(string a, int b, double c)
    {
        this.Count++;
        this.Text = $"{a}/{b}/{c}";
    }

    void IVoidService.Nullable(string? a, int? b)
    {
        this.Count++;
        this.Text = $"{a ?? "<null>"}/{(b is null ? "<null>" : b.Value.ToString())}";
    }

    void IVoidService.Generic(List<int> a, KeyValuePair<string, int> b)
    {
        this.Count++;
        this.Sum = a.Sum() + b.Value;
        this.Text = b.Key;
    }

    void IVoidService.Throw() => throw new InvalidOperationException();
}

public class BrokerVoidTest
{
    [Fact]
    public void NoReceiver()
    {// Must not throw and must do nothing.
        var radio = new RadioClass();
        radio.Send<IVoidService>().NoParameter();
        radio.Send<IVoidService>().Add(1);
        radio.GetChannel<IVoidService>().Count.Is(0);
    }

    [Fact]
    public void SingleReceiver()
    {
        var radio = new RadioClass();
        var service = new VoidService();

        using (radio.Open<IVoidService>(service))
        {
            radio.Send<IVoidService>().NoParameter();
            service.Count.Is(1);

            radio.Send<IVoidService>().Add(3);
            radio.Send<IVoidService>().Add(4);
            service.Count.Is(3);
            service.Sum.Is(7);
        }

        radio.Send<IVoidService>().Add(5);
        service.Sum.Is(7); // The link is closed.
    }

    [Fact]
    public void MultipleReceivers()
    {
        var radio = new RadioClass();
        var services = Enumerable.Range(0, 10).Select(_ => new VoidService()).ToArray();
        var links = services.Select(x => radio.Open<IVoidService>(x)).ToArray();

        radio.GetChannel<IVoidService>().Count.Is(10);
        radio.Send<IVoidService>().Add(2);

        foreach (var x in services)
        {// Every receiver must be invoked exactly once.
            x.Count.Is(1);
            x.Sum.Is(2);
        }

        foreach (var x in links)
        {
            x!.Dispose();
        }
    }

    [Fact]
    public void Parameters()
    {
        var radio = new RadioClass();
        var service = new VoidService();

        using (radio.Open<IVoidService>(service))
        {
            radio.Send<IVoidService>().Concat("a", 2, 3.5d);
            service.Text.Is("a/2/3.5");

            radio.Send<IVoidService>().Nullable(null, null);
            service.Text.Is("<null>/<null>");

            radio.Send<IVoidService>().Nullable("x", 9);
            service.Text.Is("x/9");

            radio.Send<IVoidService>().Generic([1, 2, 3,], new("key", 4));
            service.Sum.Is(10);
            service.Text.Is("key");
        }
    }

    [Fact]
    public void DefaultParameterInGlobalNamespace()
    {// A service declared in the global namespace, with a default parameter value.
        var radio = new RadioClass();
        var service = new ConductorPresentationService();

        using (radio.Open<IConductorPresentationService>(service))
        {
            radio.Send<IConductorPresentationService>().ActivateWindow();
            service.Force.IsFalse();

            radio.Send<IConductorPresentationService>().ActivateWindow(true);
            service.Force.IsTrue();
            service.Count.Is(2);
        }
    }

    [Fact]
    public void ExceptionIsPropagated()
    {// A void broker method is synchronous, so the exception must escape as-is.
        var radio = new RadioClass();

        using (radio.Open<IVoidService>(new VoidService()))
        {
            Assert.Throws<InvalidOperationException>(() => radio.Send<IVoidService>().Throw());
        }
    }

    [Fact]
    public void ClosedLinksAreSkipped()
    {// The enumeration must reach every live link, whatever its position in the internal array.
        for (var pattern = 0; pattern < 3; pattern++)
        {
            var radio = new RadioClass();
            var services = Enumerable.Range(0, 64).Select(_ => new VoidService()).ToArray();
            var links = services.Select(x => radio.Open<IVoidService>(x)!).ToArray();

            var live = new List<int>();
            for (var i = 0; i < 64; i++)
            {
                var keep = pattern switch
                {
                    0 => i == 63, // Only the last one (the internal array is scanned to the end).
                    1 => i == 0, // Only the first one.
                    _ => (i % 7) == 3, // Scattered.
                };

                if (keep)
                {
                    live.Add(i);
                }
                else
                {
                    links[i].Dispose();
                }
            }

            radio.GetChannel<IVoidService>().Count.Is(live.Count);
            radio.Send<IVoidService>().Add(5);

            for (var i = 0; i < 64; i++)
            {
                services[i].Count.Is(live.Contains(i) ? 1 : 0);
            }

            foreach (var i in live)
            {
                links[i].Dispose();
            }
        }
    }

    [Fact]
    public void DeadWeakReferencesAreSkipped()
    {// A dead weak reference must not stop the enumeration of the remaining links.
        var radio = new RadioClass();
        var service = new VoidService();

        void OpenTemporaryInstances()
        {
            for (var i = 0; i < 8; i++)
            {
                radio.Open<IVoidService>(new VoidService(), true);
            }
        }

        OpenTemporaryInstances();
        using (radio.Open<IVoidService>(service, true))
        {
            radio.GetChannel<IVoidService>().Count.Is(9);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // The collected instances are counted by CountHint, but the live one must still be invoked.
            radio.Send<IVoidService>().Add(1);
            service.Count.Is(1);
            service.Sum.Is(1);

            radio.Send<IVoidService>().Add(2);
            service.Count.Is(2);
            service.Sum.Is(3);
        }

        GC.KeepAlive(service);
    }

    [Fact]
    public void MaxLinks()
    {// IConductorPresentationService is declared with MaxLinks = 1.
        var radio = new RadioClass();
        var service1 = new ConductorPresentationService();
        var service2 = new ConductorPresentationService();

        using var link1 = radio.Open<IConductorPresentationService>(service1);
        link1.IsNotNull();

        var link2 = radio.Open<IConductorPresentationService>(service2);
        link2.IsNull(); // The channel is full.

        radio.Send<IConductorPresentationService>().ActivateWindow();
        service1.Count.Is(1);
        service2.Count.Is(0);
    }
}

public class ConductorPresentationService : IConductorPresentationService
{
    public int Count { get; private set; }

    public bool Force { get; private set; }

    void IConductorPresentationService.ActivateWindow(bool force)
    {
        this.Count++;
        this.Force = force;
    }
}
