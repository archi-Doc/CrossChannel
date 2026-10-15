// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using CrossChannel;
using Xunit;

namespace XUnitTest;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1214 // Readonly fields should appear before non-readonly fields
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS0649

public class CopyTestClass
{
    public int X { get; set; }

    public int Y { get; private set; }

    public int Z { get; init; }

    public string A = string.Empty;

    protected double B;

    private string C = string.Empty;

    private readonly string D = string.Empty;

    private readonly object E;

    private readonly object F;

    private readonly long G;

    private readonly byte[] H;

    public void Prepare()
    {
        this.X = 1;
        this.Y = 2;
        typeof(CopyTestClass).GetMethod("set_Z", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(this, [3]);
        this.A = "3";
        this.B = 4.44;
        this.C = "5";
        Unsafe.AsRef<string>(in this.D) = "99";
        Unsafe.AsRef<object>(in this.E) = 2.55d;
        Unsafe.AsRef<object>(in this.F) = this;
        Unsafe.AsRef<long>(in this.G) = 1234;
        Unsafe.AsRef<byte[]>(in this.H) = [1, 2, 3, 44, 55, 66, 77, 222, 9];
    }

    public bool Compare(CopyTestClass other)
    {
        return this.X == other.X
            && this.Y == other.Y
            && this.Z == other.Z
            && this.A == other.A
            && this.B == other.B
            && this.C == other.C
            && this.D == other.D
            && this.E == other.E
            && this.F == other.F
            && this.G == other.G
            && this.H.AsSpan().SequenceEqual(other.H);
    }
}

public class FieldCopierTest
{
    [Fact]
    public void Test1()
    {
        var tc = new CopyTestClass();
        tc.Prepare();

        var tc2 = new CopyTestClass();
        FieldCopier.Copy(ref tc, ref tc2);
        tc.Compare(tc2).IsTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CopiesPrivateFieldsAcrossInheritance(bool createDelegate)
    {
        var from = new DerivedCopyTarget(42, "base", "derived");
        var to = new DerivedCopyTarget(0, string.Empty, string.Empty);
        var original = to;

        if (createDelegate)
        {
            FieldCopier.GetDelegate<DerivedCopyTarget>()(ref from, ref to);
        }
        else
        {
            FieldCopier.Copy(ref from, ref to);
        }

        Assert.Same(original, to);
        Assert.Equal(42, to.Id);
        Assert.Equal("base", to.BaseText);
        Assert.Equal("derived", to.Text);
    }

    [Fact]
    public void StringIsRejected()
    {// Copying the length field of a string would make the destination read past its own memory.
        var source = new string('a', 3);
        var destination = new string('b', 2);
        Assert.Throws<NotSupportedException>(() => FieldCopier.Copy(ref source, ref destination));
        Assert.Throws<NotSupportedException>(() => FieldCopier.GetDelegate<string>()(ref source, ref destination));
        Assert.Equal("bb", destination);
    }

    [Fact]
    public void TypeWithoutFieldsIsANoOp()
    {
        var source = new NoFieldTarget();
        var destination = new NoFieldTarget();
        var original = destination;
        FieldCopier.Copy(ref source, ref destination);
        Assert.Same(original, destination);
        Assert.Same(FieldCopier.GetDelegate<NoFieldTarget>(), FieldCopier.GetDelegate<NoFieldTarget>());

        var sourceObject = new object();
        var destinationObject = new object();
        FieldCopier.Copy(ref sourceObject, ref destinationObject);
        FieldCopier.GetDelegate<object>()(ref sourceObject, ref destinationObject);
        Assert.NotSame(sourceObject, destinationObject);
    }

    [Fact]
    public void CopyThroughABaseTypeIgnoresSubtypeFields()
    {// The fields are those of T, not of the runtime type of the instances.
        BaseFieldTarget source = new DerivedFieldTarget { A = 1, B = 2, };
        BaseFieldTarget destination = new DerivedFieldTarget();
        FieldCopier.Copy(ref source, ref destination);
        Assert.Equal(1, destination.A);
        Assert.Equal(0, ((DerivedFieldTarget)destination).B);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public unsafe void CopiesReadonlyPointerFieldsDeclaredByABaseType(bool createDelegate)
    {// A pointer type cannot be a generic argument, so readonly pointer fields need their own path in the compiled delegate.
        var source = new DerivedPointerTarget((byte*)0x1234, &PointerTargetFunction, (byte*)0x5678, 7, "derived");
        var destination = new DerivedPointerTarget(null, null, null, 0, string.Empty);
        if (createDelegate)
        {
            FieldCopier.GetDelegate<DerivedPointerTarget>()(ref source, ref destination);
        }
        else
        {
            FieldCopier.Copy(ref source, ref destination);
        }

        Assert.Equal((nint)0x1234, (nint)destination.Pointer);
        Assert.Equal((nint)source.Function, (nint)destination.Function);
        Assert.NotEqual((nint)0, (nint)destination.Function);
        Assert.Equal((nint)0x5678, (nint)destination.MutablePointer);
        Assert.Equal(7, destination.Value);
        Assert.Equal("derived", destination.Text);
        Assert.Same(FieldCopier.GetDelegate<DerivedPointerTarget>(), FieldCopier.GetDelegate<DerivedPointerTarget>());
    }

    [Fact]
    public unsafe void ReflectionDelegateMatchesCompiledDelegate()
    {// The reflection delegate is used under Native AOT; run it under JIT as well (through private reflection).
        var getFields = typeof(FieldCopier).GetMethod("GetFields", BindingFlags.Static | BindingFlags.NonPublic, [typeof(Type)]);
        Assert.True(getFields is not null, "FieldCopier.GetFields(Type) was not found; update this test if it was renamed.");
        var createReflectionDelegate = typeof(FieldCopier).GetMethod("CreateReflectionDelegate", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.True(createReflectionDelegate is { IsGenericMethodDefinition: true }, "FieldCopier.CreateReflectionDelegate<T>(FieldInfo[]) was not found; update this test if it was renamed.");

        FieldCopier.CopyDelegate<T> CreateReflectionDelegate<T>()
            where T : class
        {
            var fields = getFields!.Invoke(null, [typeof(T)]);
            Assert.IsType<FieldInfo[]>(fields);
            return Assert.IsType<FieldCopier.CopyDelegate<T>>(createReflectionDelegate!.MakeGenericMethod(typeof(T)).Invoke(null, [fields]));
        }

        var tc = new CopyTestClass();
        tc.Prepare();
        var tc2 = new CopyTestClass();
        CreateReflectionDelegate<CopyTestClass>()(ref tc, ref tc2);
        tc.Compare(tc2).IsTrue();

        var from = new DerivedCopyTarget(42, "base", "derived");
        var to = new DerivedCopyTarget(0, string.Empty, string.Empty);
        CreateReflectionDelegate<DerivedCopyTarget>()(ref from, ref to);
        Assert.Equal(42, to.Id);
        Assert.Equal("base", to.BaseText);
        Assert.Equal("derived", to.Text);

        var source = new DerivedPointerTarget((byte*)0x1234, &PointerTargetFunction, (byte*)0x5678, 7, "derived");
        var destination = new DerivedPointerTarget(null, null, null, 0, string.Empty);
        CreateReflectionDelegate<DerivedPointerTarget>()(ref source, ref destination);
        Assert.Equal((nint)0x1234, (nint)destination.Pointer);
        Assert.Equal((nint)source.Function, (nint)destination.Function);
        Assert.Equal((nint)0x5678, (nint)destination.MutablePointer);
        Assert.Equal(7, destination.Value);
        Assert.Equal("derived", destination.Text);
    }

    private static void PointerTargetFunction()
    {
    }

    private sealed class NoFieldTarget
    {
        public int Value => 1;
    }

    private class BaseFieldTarget
    {
        public int A { get; set; }
    }

    private sealed class DerivedFieldTarget : BaseFieldTarget
    {
        public int B { get; set; }
    }

    private unsafe class BasePointerTarget
    {
        private readonly byte* pointer;
        private readonly delegate*<void> function;
        private readonly int value;

        protected BasePointerTarget(byte* pointer, delegate*<void> function, byte* mutablePointer, int value)
        {
            this.pointer = pointer;
            this.function = function;
            this.MutablePointer = mutablePointer;
            this.value = value;
        }

        public byte* Pointer => this.pointer;

        public delegate*<void> Function => this.function;

        public byte* MutablePointer { get; private set; }

        public int Value => this.value;
    }

    private sealed unsafe class DerivedPointerTarget : BasePointerTarget
    {
        private readonly string text;

        public DerivedPointerTarget(byte* pointer, delegate*<void> function, byte* mutablePointer, int value, string text)
            : base(pointer, function, mutablePointer, value)
        {
            this.text = text;
        }

        public string Text => this.text;
    }

    private class BaseCopyTarget
    {
        private readonly int id;
        private readonly string text;

        protected BaseCopyTarget(int id, string text)
        {
            this.id = id;
            this.text = text;
        }

        public int Id => this.id;

        public string BaseText => this.text;
    }

    private class MiddleCopyTarget : BaseCopyTarget
    {
        protected MiddleCopyTarget(int id, string text)
            : base(id, text)
        {
        }
    }

    private sealed class DerivedCopyTarget : MiddleCopyTarget
    {
        private readonly string text;

        public DerivedCopyTarget(int id, string baseText, string text)
            : base(id, baseText)
        {
            this.text = text;
        }

        public string Text => this.text;
    }
}
