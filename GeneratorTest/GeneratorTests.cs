// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using CrossChannel;
using CrossChannel.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace GeneratorTest;

public class GeneratorTests
{
    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(System.IO.Path.PathSeparator)
        .Append(typeof(Radio).Assembly.Location)
        .Where(x => !string.Equals(x, typeof(CrossChannelGeneratorV2).Assembly.Location, StringComparison.OrdinalIgnoreCase))
        .Distinct()
        .Select(x => MetadataReference.CreateFromFile(x))
        .ToArray();

    [Theory]
    [InlineData("[RadioService] public interface IService { void M(); }", "CCG002")]
    [InlineData("[RadioService] public interface IService : IRadioService { int M(); }", "CCG003")]
    [InlineData("public class Host { [RadioService] public interface IService : IRadioService { void M(); } }", "CCG001")]
    [InlineData("[RadioService] public interface IService<T> : IRadioService { void M(); }", "CCG004")]
    [InlineData("public partial class Host<T> { [RadioService] public interface IService : IRadioService { void M(); } }", "CCG004")]
    [InlineData("[RadioService] public interface IService : IRadioService { void M<T>(T x); }", "CCG004")]
    [InlineData("[RadioService] public interface IService : IRadioService { void M(ref int x); }", "CCG004")]
    [InlineData("[RadioService] public interface IService : IRadioService { void M(out int x); }", "CCG004")]
    [InlineData("[RadioService] public interface IService : IRadioService { void M(in int x); }", "CCG004")]
    [InlineData("[RadioService] public interface IService : IRadioService { int Value { get; } }", "CCG004")]
    [InlineData("[RadioService] public interface IService : IRadioService { event System.Action E; }", "CCG004")]
    [InlineData("[RadioService] public interface IService : IRadioService { static abstract void M(); }", "CCG004")]
    [InlineData("[RadioService] public interface IService : IRadioService { ref RadioResult<int> M(); }", "CCG004")]
    public void InvalidDeclarationsReportDiagnostics(string source, string id)
    {
        var result = Run("using CrossChannel; " + source);
        Assert.Contains(result.Diagnostics, x => x.Id == id);
        Assert.DoesNotContain(result.Diagnostics, x => x.Id == "CS8785");
    }

    [Fact]
    public void SupportedSignaturesCompile()
    {
        var result = Run("""
            #nullable enable
            using CrossChannel;
            using System.Threading.Tasks;
            using Service = CrossChannel.RadioServiceAttribute;
            public interface IBase : IRadioService { RadioResult<int> Value(); }
            [Service]
            public interface IService : IBase
            {
                new RadioResult<int> Value();
                void @event(string? value);
                RadioResult<int[]> Arrays();
                RadioResult<int[,]> Matrices();
                RadioResult<(int X, string? Y)> Tuples();
                Task<RadioResult<string?[]>> ArraysAsync();
                static void Helper() { }
            }
            """);
        Assert.NotEmpty(result.GeneratedTrees);
        Assert.DoesNotContain(result.Diagnostics, x => x.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(result.Output.GetDiagnostics(TestContext.Current.CancellationToken), x => x.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void InheritedUnsupportedMembersReportDiagnostics()
    {
        var result = Run("""
            using CrossChannel;
            public interface IBase : IRadioService { int Value { get; } }
            [RadioService] public interface IService : IBase { void M(); }
            """);
        Assert.Contains(result.Diagnostics, x => x.Id == "CCG004");
    }

    [Fact]
    public void NestedStructAndMultipleNamespacesCompile()
    {
        var result = Run("""
            using CrossChannel;
            namespace First
            {
                public partial struct Host
                {
                    [RadioService] public interface IService : IRadioService { void M(); }
                }
            }
            namespace Second
            {
                [RadioService] public interface IService : IRadioService { void M(); }
            }
            """);
        Assert.Equal(2, result.GeneratedTrees.Length);
        Assert.DoesNotContain(result.Diagnostics, x => x.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(result.Output.GetDiagnostics(TestContext.Current.CancellationToken), x => x.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void UnrelatedAttributeDoesNotGenerateBroker()
    {
        var result = Run("""
            using CrossChannel;
            [System.Obsolete] public interface IService : IRadioService { void M(); }
            """);
        Assert.Empty(result.GeneratedTrees);
        Assert.Empty(result.Diagnostics);
    }

    private static (Compilation Output, Diagnostic[] Diagnostics, SyntaxTree[] GeneratedTrees) Run(string source)
    {
        var compilation = CSharpCompilation.Create("GeneratorInput",
            new[] { CSharpSyntaxTree.ParseText(source) }, References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new CrossChannelGeneratorV2().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        return (output, diagnostics.ToArray(), driver.GetRunResult().GeneratedTrees.ToArray());
    }
}
