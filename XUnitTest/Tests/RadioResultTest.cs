// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
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
        empty.TryGetFirst(out _).IsFalse();
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
        single.TryGetFirst(out var r).IsTrue();
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
        var result = RadioResult<string?>.FromValue(null);
        result.Count.Is(1);
        result.GetHashCode().Is(0);
        result.ToString().Is("[]");
        result.TryGetFirst(out var r).IsTrue();
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

    [Fact]
    public void InequalityOperatorStructEnumeratorAndObjectEquals()
    {
        (new RadioResult<int>([1, 2,]) != new RadioResult<int>(1)).IsTrue();
        (new RadioResult<int>([1, 2,]) != new RadioResult<int>([1, 2,])).IsFalse(); // Two separate arrays with the same elements.
        (new RadioResult<int>([1, 2,]) != new RadioResult<int>([1, 3,])).IsTrue();
        (new RadioResult<int>(1) != default(RadioResult<int>)).IsTrue();
        (RadioResult<int>.Empty != default(RadioResult<int>)).IsFalse();

        // foreach binds to the public struct enumerator.
        Enumerate(default).Count.Is(0);
        Enumerate(new(5)).SequenceEqual([5,]).IsTrue();
        Enumerate(new([1, 2, 3,])).SequenceEqual([1, 2, 3,]).IsTrue();

        ((object)new RadioResult<int>(1)).Equals(1).IsFalse(); // A boxed int is not a RadioResult.
        new RadioResult<int>(1).Equals((object?)null).IsFalse();

        // An array value is a single result when T is an array type.
        RadioResult<int[]>.FromValue([1, 2,]).Count.Is(1);
        new RadioResult<int[]>(new[] { 1, 2, }).Count.Is(1);

        static List<int> Enumerate(RadioResult<int> result)
        {
            var list = new List<int>();
            RadioResult<int>.Enumerator enumerator = result.GetEnumerator();
            while (enumerator.MoveNext())
            {
                list.Add(enumerator.Current);
            }

            foreach (var x in result)
            {
                list.Add(x);
            }

            list.Count.Is(result.Count * 2);
            return list.Take(result.Count).ToList();
        }
    }

    [Fact]
    public void CovariantArray()
    {// An array whose runtime type is an array of a derived type (string[] as object[]) is retained as is; Equals must not throw.
        string[] strings = ["a", "b",];
        var covariant = new RadioResult<object>(strings);
        var exact = new RadioResult<object>(new object[] { "a", "b", });
        covariant.Count.Is(2);

        (covariant == exact).IsTrue();
        (exact == covariant).IsTrue();
        (covariant == new RadioResult<object>(strings)).IsTrue();
        (covariant != new RadioResult<object>(new object[] { "a", "c", })).IsTrue();
        covariant.Equals((object)exact).IsTrue();
        EqualityComparer<RadioResult<object>>.Default.Equals(covariant, exact).IsTrue();
        covariant.GetHashCode().Is(exact.GetHashCode());
        var dictionary = new Dictionary<RadioResult<object>, int> { [covariant] = 1, };
        dictionary[exact].Is(1);
        new HashSet<RadioResult<object>> { exact, }.Contains(covariant).IsTrue();
        covariant.ToString().Is("[a, b]");
    }

    [Fact]
    public void FromValueWithObject()
    {// When T is object (or an interface that arrays implement), the constructor binds an array or null to the array overload.
        var array = new string[] { "a", "b", };
        var single = RadioResult<object>.FromValue(array);
        single.Count.Is(1);
        single.TryGetFirst(out var first).IsTrue();
        ReferenceEquals(first, array).IsTrue();
        new RadioResult<object>(array).Count.Is(2);

        RadioResult<IEnumerable>.FromValue(array).Count.Is(1);
        new RadioResult<IEnumerable>(array).Count.Is(2);

        var nullValue = RadioResult<object?>.FromValue(null);
        nullValue.Count.Is(1);
        nullValue.TryGetFirst(out var value).IsTrue();
        value.IsNull();
        Assert.Throws<ArgumentNullException>(() => new RadioResult<object?>(null!));
    }
}
