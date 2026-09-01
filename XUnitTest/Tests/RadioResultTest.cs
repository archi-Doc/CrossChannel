// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using CrossChannel;
using Xunit;

namespace XUnitTest;

public class RadioResultTest
{
    [Fact]
    public void Empty()
    {
        var empty = default(RadioResult<int>);
        empty.IsEmpty.IsTrue();
        empty.Count.Is(0);
        empty.TryGetSingleResult(out _).IsFalse();
        empty.ToArray().Length.Is(0);
        empty.ToString().Is("[]");
        (empty == RadioResult<int>.Empty).IsTrue();
    }

    [Fact]
    public void SingleAndArray()
    {
        var single = new RadioResult<int>(5);
        single.IsEmpty.IsFalse();
        single.Count.Is(1);
        single.TryGetSingleResult(out var r).IsTrue();
        r.Is(5);
        single.SequenceEqual([5,]).IsTrue();
        single.ToString().Is("[5]");

        var array = new RadioResult<int>([1, 2, 3,]);
        array.Count.Is(3);
        array.SequenceEqual([1, 2, 3,]).IsTrue();
        array.ToString().Is("[1, 2, 3]");

        // An array with a single element is collapsed into a single result.
        (new RadioResult<int>([9,]) == new RadioResult<int>(9)).IsTrue();

        // An empty array is collapsed into an empty result.
        new RadioResult<int>([]).IsEmpty.IsTrue();
    }

    [Fact]
    public void EqualsBetweenDifferentStates()
    {// Comparing an array result with a single result must not throw.
        var empty = default(RadioResult<int>);
        var single = new RadioResult<int>(5);
        var array = new RadioResult<int>([1, 2,]);

        array.Equals(single).IsFalse();
        single.Equals(array).IsFalse();
        array.Equals(empty).IsFalse();
        empty.Equals(array).IsFalse();
        single.Equals(empty).IsFalse();
        empty.Equals(single).IsFalse();

        // Boxed comparison (ValueType.Equals must not be used).
        ((object)array).Equals(single).IsFalse();
        ((object)single).Equals(new RadioResult<int>(5)).IsTrue();

        // Collection operations must not throw.
        new List<RadioResult<int>> { array, }.Contains(single).IsFalse();
        var dictionary = new Dictionary<RadioResult<int>, string> { [empty] = "empty", [single] = "single", [array] = "array", };
        dictionary[new RadioResult<int>([1, 2,])].Is("array");
        dictionary[new RadioResult<int>(5)].Is("single");
        dictionary[default].Is("empty");
    }

    [Fact]
    public void NullResult()
    {
        var result = RadioResult<string?>.Single(null);
        result.Count.Is(1);
        result.GetHashCode().Is(0);
        result.ToString().Is("[]");
        result.TryGetSingleResult(out var r).IsTrue();
        r.IsNull();

        var array = RadioResult<string?>.FromArray([null, "a",]);
        array.Count.Is(2);
        array.ToString().Is("[, a]");
        array.GetHashCode(); // Must not throw.
    }

    [Fact]
    public void HashCode()
    {// The hash code must be order-sensitive, since Equals compares the sequence.
        new RadioResult<int>([1, 2,]).GetHashCode().IsNot(new RadioResult<int>([2, 1,]).GetHashCode());
        new RadioResult<int>([1, 2,]).GetHashCode().Is(new RadioResult<int>([1, 2,]).GetHashCode());
        default(RadioResult<int>).GetHashCode().Is(0);
    }
}
