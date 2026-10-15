// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CrossChannel;
using CrossChannel.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace GeneratorTest;

public class GeneratorTests
{
    private const string Header = """
        #nullable enable
        using System.Threading.Tasks;
        using CrossChannel;

        """;

    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(System.IO.Path.PathSeparator)
        .Append(typeof(Radio).Assembly.Location)
        .Where(x => !string.Equals(x, typeof(CrossChannelGenerator).Assembly.Location, StringComparison.OrdinalIgnoreCase))
        .Distinct()
        .Select(x => MetadataReference.CreateFromFile(x))
        .ToArray();

    [Theory]
    [InlineData("[RadioService] public interface IService { void M(); }", "CCG002")]
    [InlineData("[RadioService] public interface IService : IRadioService { int M(); }", "CCG003")]
    [InlineData("[RadioService] public interface IService : IRadioService { RadioResult<dynamic> M(); }", "CCG003")]
    [InlineData("[RadioService] public interface IService : IRadioService { System.Threading.Tasks.Task<dynamic> M(); }", "CCG003")]
    [InlineData("[RadioService] public interface IService : IRadioService { System.Threading.Tasks.Task<RadioResult<dynamic>> M(); }", "CCG003")]
    [InlineData("[RadioService] public interface IService : IRadioService { System.Action M(); }", "CCG003")]
    [InlineData("[RadioService] public interface IService : IRadioService { dynamic M(); }", "CCG003")]
    [InlineData("[RadioService] public interface IService : IRadioService { System.Threading.Tasks.Task<int> M(); }", "CCG003")]
    [InlineData("[RadioService] public interface IService : IRadioService { System.Threading.Tasks.ValueTask M(); }", "CCG003")]
    [InlineData("[RadioService] public interface IService : IRadioService { Unknown M(); }", "CCG003")]
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
    [InlineData("[RadioService] public interface IService : IRadioService { static abstract IService operator +(IService a, IService b); }", "CCG004")]
    [InlineData("[RadioService] public interface IService : IRadioService { ref RadioResult<int> M(); }", "CCG004")]
    [InlineData("[RadioService] public interface IService : IRadioService { void M(); protected void P(); }", "CCG004")]
    [InlineData("[RadioService] public interface IService : IRadioService { void M(); private protected void P(); }", "CCG004")]
    [InlineData("[RadioService] file interface IService : IRadioService { void M(); }", "CCG005")]
    [InlineData("file partial class Host { [RadioService] public interface IService : IRadioService { void M(); } }", "CCG005")]
    public void InvalidDeclarationsReportDiagnostics(string source, string id)
    {
        var result = Run("using CrossChannel; " + source);
        Assert.Contains(result.Diagnostics, x => x.Id == id);
        Assert.DoesNotContain(result.Diagnostics, x => x.Id == "CS8785");
        Assert.Empty(result.GeneratedTrees);
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
                RadioResult<System.Action> Delegates();
                Task<RadioResult<System.Func<int>?>> DelegatesAsync();
                static void Helper() { }
            }
            """);
        Assert.NotEmpty(result.GeneratedTrees);
        AssertCompilesCleanly(result);
    }

    [Fact]
    public void TaskAndMultiParameterMethodsCompile()
    {
        var result = Run(Header + """
            [RadioService]
            public interface IService : IRadioService
            {
                Task T(int a, string? b);
                void V(int a, int b);
                RadioResult<int> R(int a, int b);
                Task<RadioResult<int>> A(int a, string? b);
            }
            """);
        AssertCompilesCleanly(result);
        var text = result.GeneratedTrees.Single().ToString();
        Assert.Contains("Task.WhenAll(global::System.MemoryExtensions.AsSpan(tasks!, 0, count))", text);
        Assert.Contains("global::CrossChannel.RadioTask.AggregateAsync<int>(tasks!, count)", text);
    }

    [Theory]
    [InlineData("namespace N { [RadioService] public partial interface IService : IRadioService { void M(); } }")] // Partial root service
    [InlineData("[RadioService] public partial interface IService : IRadioService { void M(); }")] // Partial root service in the global namespace
    [InlineData("namespace N { public partial interface IHost { [RadioService] public interface IService : IRadioService { void M(); } } }")] // Partial interface container
    [InlineData("namespace N { [RadioService] public partial interface IOuter : IRadioService { void M(); [RadioService] public interface IInner : IRadioService { void N(); } } }")] // Partial service hosting a service
    [InlineData("namespace N { [RadioService] public interface IService : IRadioService, System.IProgress<int>, System.IObserver<string>, System.IDisposable { } }")] // Bases in the System namespace
    [InlineData("namespace X.System { [RadioService] public interface IService : IRadioService { void M(); Task T(); Task<RadioResult<int>> R(); RadioResult<string> S(); } }")] // Namespace X.System
    [InlineData("namespace X.System { public class Marker { } } namespace X.Svc { [RadioService] public interface IService : IRadioService { void M(); Task T(); Task<RadioResult<int>> R(); } }")] // Sibling namespace X.System
    [InlineData("namespace X.CrossChannel.Y { [RadioService] public interface IService : IRadioService { void M(); Task<RadioResult<int>> R(); RadioResult<int> S(); } }")] // Namespace X.CrossChannel.Y
    [InlineData("namespace N { public class GameSystem { } public partial class World { public GameSystem System { get; } = new(); [RadioService] public interface IService : IRadioService { void A(int x); RadioResult<string> B(); Task C(); Task<RadioResult<int>> D(); } } }")] // Host with a System member
    [InlineData("namespace N { public class Channel<T> { } public class RadioServiceRegistry { } public class RadioTask { } [RadioService] public interface IService : IRadioService { void M(); Task<RadioResult<int>> R(); } }")] // User types named like the helpers
    [InlineData("namespace Foo { public class Foo { } [RadioService] public interface IService : IRadioService { void M(); } public partial class Host { [RadioService] public interface INested : IRadioService { void M(); } } }")] // Type named like its namespace
    [InlineData("public interface IBase<T> { void M(T x); RadioResult<T> R(T x); } [RadioService] public interface IService : IRadioService, IBase<string?> { }")] // Nullable type argument of a base interface
    [InlineData("public class Outer<T> { public class Inner { } } [RadioService] public interface IService : IRadioService { void F(Outer<string?>.Inner i); }")] // Nullable type argument of a containing type
    [InlineData("[RadioService] public interface IService : IRadioService { void M((int, int, int, int, int, int, int, int, int) v); RadioResult<(int A, string? B, int C, int D, int E, int F, int G, int H, int I)> R(); }")] // Tuples with more than 7 elements
    [InlineData("namespace N { public class @event { } [RadioService] public interface @interface : IRadioService { void @class(@event e); RadioResult<@event> @void(); } public partial class @struct { [RadioService] public interface INested : IRadioService { void M(); } } }")] // Keyword names
    [InlineData("[RadioService] public interface IService : IRadioService { RadioResult<int[]> H(dynamic? d); void G(dynamic d, int x); }")] // Dynamic parameters
    [InlineData("[RadioService] public interface IService : IRadioService { void M([System.Diagnostics.CodeAnalysis.AllowNull] string s, [System.Diagnostics.CodeAnalysis.DisallowNull] string? t); }")] // Nullable preconditions
    [InlineData("[RadioService] public interface IService : IRadioService { void A([System.Diagnostics.CodeAnalysis.NotNull] string? s); void B([System.Diagnostics.CodeAnalysis.MaybeNull] string s); [return: System.Diagnostics.CodeAnalysis.MaybeNull] Task C(); [return: System.Diagnostics.CodeAnalysis.NotNull] Task? D(); [return: System.Diagnostics.CodeAnalysis.MaybeNull] Task<RadioResult<int>> E(); }")] // Nullable postconditions
    [InlineData("[RadioService] public interface IService : IRadioService { [System.Obsolete] void A(); [System.Obsolete(\"x\")] Task B(); [System.Obsolete(\"x\", true)] RadioResult<int> C(); [System.Obsolete(\"x\", error: true)] Task<RadioResult<int>> D(); void E(); }")] // Obsolete methods
    [InlineData("[System.Obsolete][RadioService] public interface IService : IRadioService { void M(); } [System.Obsolete] public interface IOldBase : IRadioService { void B(); } [RadioService] public interface IDerived : IOldBase { void M(); }")] // Obsolete interfaces
    [InlineData("[System.Obsolete(\"x\", DiagnosticId = \"LIB001\")][RadioService] public interface IService : IRadioService { void M(OldType x); RadioResult<OldType> R(); } [System.Obsolete(\"y\", DiagnosticId = \"LIB002\")] public class OldType { }")] // Obsolete with custom diagnostic ids
    [InlineData("[System.Obsolete(\"x\", true)][RadioService] public interface IService : IRadioService { void M(); } public partial class Host { [System.Obsolete(\"y\", true)][RadioService] public interface INested : IRadioService { void M(); } }")] // Obsolete interfaces with error: true
    [InlineData("[RadioService] public interface IService : IRadioService { Task? M(); Task<RadioResult<string?>>? N(); }")] // Nullable task returns
    [InlineData("public interface IBase : IRadioService { void X(); } public interface IBase2 : IRadioService { void B(); } [RadioService] public interface IService : IBase, IBase2 { void K(); private void H() { } sealed void S() { } protected void PD() { } void IBase.X() { } abstract void IBase2.B(); static void Helper() { } static virtual void V() { } internal void I(); protected internal void PI(); }")] // Members which are skipped or brokered
    [InlineData("[RadioService] public interface IService : IRadioService { RadioResult<System.Action> A(); Task<RadioResult<System.Func<int>?>> B(); RadioResult<System.Func<string?, int>[]> C(); }")] // Delegate result types
    [InlineData("namespace N { public partial class Host { [RadioService] public interface IA : IRadioService { void M(); } [RadioService] public interface IB : IRadioService { void M(); } } public partial class Outer { public partial class Inner { [RadioService] public interface IC : IRadioService { void M(); } } private partial struct PrivateInner { [RadioService] public interface ID : IRadioService { void M(); } } } }")] // Sibling and deeply nested services
    public void SupportedShapesCompileCleanly(string source)
    {
        var result = Run(Header + source);
        Assert.NotEmpty(result.GeneratedTrees);
        AssertCompilesCleanly(result);
    }

    [Fact]
    public void PointerParametersCompileInUnsafeCode()
    {
        var result = Run(Header + """
            [RadioService] public unsafe interface IService : IRadioService { void A(int* p); void B(delegate*<int, void> f); RadioResult<int> C(int*[] p); Task D(int* p); Task<RadioResult<int>> E(int* p); }
            [RadioService] public interface IMember : IRadioService { void M(); unsafe void P(byte* p); }
            [RadioService] public interface ISafe : IRadioService { void M(); }
            """, allowUnsafe: true);
        AssertCompilesCleanly(result);

        // Only the brokers which need it are unsafe, since 'unsafe' requires AllowUnsafeBlocks.
        var text = result.GeneratedTrees.Single().ToString();
        Assert.Equal(2, text.Split("private sealed unsafe class").Length - 1);
        Assert.Equal(1, text.Split("private sealed class").Length - 1);
    }

    [Fact]
    public void GlobalUsingOfSystemThreadingChannelsCompiles()
    {// System.Threading.Channels.Channel<T> must not be confused with CrossChannel.Channel<TService>.
        var result = Run("""
            global using System.Threading.Channels;
            using CrossChannel;
            namespace N
            {
                [RadioService] public interface IService : IRadioService { void M(); }
                public partial class Host { [RadioService] public interface INested : IRadioService { void M(); } }
            }
            """);
        Assert.Single(result.GeneratedTrees);
        AssertCompilesCleanly(result);
    }

    [Fact]
    public void PartialContainersAreInitialized()
    {// The root initializer calls the initializer of every partial type which hosts nested services, interfaces included.
        var result = Run(Header + """
            namespace N
            {
                [RadioService] public partial interface IRoot : IRadioService { void M(); }
                public partial interface IHost { [RadioService] public interface IService : IRadioService { void M(); } }
                public partial class Host { public partial class Inner { [RadioService] public interface IService : IRadioService { void M(); } } }
            }
            """);
        AssertCompilesCleanly(result);
        var text = result.GeneratedTrees.Single().ToString();
        Assert.Contains("RadioServiceRegistry.Register(new(typeof(global::N.IRoot)", text);
        Assert.Contains("global::N.IHost.__Initialize__();", text);
        Assert.Contains("global::N.Host.__Initialize__();", text);
        Assert.Contains("global::N.Host.Inner.__Initialize__();", text);
        Assert.DoesNotContain("partial interface IRoot", text); // A partial service without nested services needs no partial declaration.
        Assert.Contains("internal static class __CrossChannelRoot__", text);
    }

    [Fact]
    public void GeneratedCodeNeedsNoDocumentationComments()
    {// The generated code declares no public member, so CS1591 is never reported in it, even for an undocumented public host.
        var result = Run(
            Header + """
            namespace N
            {
                public partial class Host { [RadioService] public interface IService : IRadioService { void M(); } }
                public partial interface IHost { [RadioService] public interface IService : IRadioService { void M(); } }
                [RadioService] public interface IRoot : IRadioService { void M(); }
            }
            """,
            documentationMode: DocumentationMode.Diagnose);
        Assert.Contains(result.Output.GetDiagnostics(TestContext.Current.CancellationToken), x => x.Id == "CS1591"); // Reported in the user code.
        AssertCompilesCleanly(result);
    }

    [Fact]
    public void NamespacesGetValidAndUniqueHintNames()
    {// Hint names may not contain '@' and must be unique ignoring case.
        var result = Run(Header + """
            namespace @class.@void { [RadioService] public interface IService : IRadioService { void M(); } }
            namespace CaseA { [RadioService] public interface IService : IRadioService { void M(); } }
            namespace casea { [RadioService] public interface IService : IRadioService { void M(); } }
            """);
        Assert.Equal(3, result.GeneratedTrees.Length);
        Assert.Equal(3, result.GeneratedTrees.Select(x => x.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        AssertCompilesCleanly(result);
    }

    [Fact]
    public void AttributeArgumentsAreEmitted()
    {
        var result = Run(Header + """
            [CrossChannelGeneratorOptions(AttachDebugger = true)] public interface IOptions { }
            [RadioService(MaxLinks = 3, AutoRegisterServiceAndSender = false)] public interface IService : IRadioService { void M(); }
            """);
        AssertCompilesCleanly(result);
        Assert.Contains(", 3, false));", result.GeneratedTrees.Single().ToString());
    }

    [Fact]
    public void MaxLinksIsFormattedWithTheInvariantCulture()
    {// Some cultures (e.g. sv-SE with ICU) use U+2212 as the negative sign, which is not valid C#.
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "−";
        var previous = CultureInfo.CurrentCulture;
        (Compilation Output, Diagnostic[] Diagnostics, SyntaxTree[] GeneratedTrees) result;
        try
        {
            CultureInfo.CurrentCulture = culture;
            result = Run(Header + "[RadioService(MaxLinks = -1)] public interface IService : IRadioService { void M(); }");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }

        AssertCompilesCleanly(result);
        Assert.Contains(", -1, true));", result.GeneratedTrees.Single().ToString());
    }

    [Fact]
    public void RuntimeAttributeDefaultsMatchGeneratorMocks()
    {
        var attribute = new RadioServiceAttribute();
        var mock = new RadioServiceAttributeMock();
        Assert.Equal(attribute.MaxLinks, mock.MaxLinks);
        Assert.Equal(attribute.AutoRegisterServiceAndSender, mock.AutoRegisterServiceAndSender);

        var options = new CrossChannelGeneratorOptionsAttribute();
        var optionsMock = new CrossChannelGeneratorOptionsAttributeMock();
        Assert.Equal(options.AttachDebugger, optionsMock.AttachDebugger);
        Assert.Equal(options.GenerateToFile, optionsMock.GenerateToFile);
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
        AssertCompilesCleanly(result);
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

    [Theory]
    [InlineData("namespace N { [CrossChannel.RadioService] public interface IService { void M(); } }")] // The attribute is not resolved.
    [InlineData("namespace CrossChannel { public sealed class RadioServiceAttribute : System.Attribute { } [RadioService] public interface IService { void M(); } }")] // No IRadioService
    public void NoCrossChannelReferenceGeneratesNothing(string source)
    {
        var references = References.Where(x => !string.Equals(x.Display, typeof(Radio).Assembly.Location, StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Equal(References.Length - 1, references.Length);

        var result = Run(source, references: references);
        Assert.Empty(result.GeneratedTrees);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void GeneratorOptionsInATreeWithoutAFilePathStillGenerate()
    {// An in-memory syntax tree has no folder, so the output falls back to the compilation.
        var result = Run("""
            using CrossChannel;
            [CrossChannelGeneratorOptions(GenerateToFile = true)] public interface IOptions { }
            [RadioService] public interface IService : IRadioService { void M(); }
            """);
        Assert.Single(result.GeneratedTrees);
        AssertCompilesCleanly(result);
    }

    [Fact]
    public void GenerateToFileWritesIntoAnExistingGeneratedFolder()
    {
        var directory = Path.Combine(Path.GetTempPath(), "CrossChannelGeneratorTest_" + Guid.NewGuid().ToString("N"));
        var generated = Path.Combine(directory, "Generated");
        Directory.CreateDirectory(generated);
        try
        {
            var result = Run(
                """
                using CrossChannel;
                namespace N
                {
                    [CrossChannelGeneratorOptions(GenerateToFile = true)] public interface IOptions { }
                    [RadioService] public interface IService : IRadioService { void M(); }
                }
                """,
                Path.Combine(directory, "Options.cs"));
            Assert.Empty(result.GeneratedTrees); // Written to the folder instead of added to the compilation.
            Assert.DoesNotContain(result.Diagnostics, x => x.Severity >= DiagnosticSeverity.Warning);

            var file = Path.Combine(generated, "gen.CrossChannel.N.cs");
            Assert.True(File.Exists(file));

            // The written source compiles together with the input.
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file, cancellationToken: TestContext.Current.CancellationToken);
            AssertCompilesCleanly((result.Output.AddSyntaxTrees(tree), result.Diagnostics, [tree]));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void GeneratorHasNoInstanceState()
    {// The host may share one generator instance between compilations running concurrently.
        Assert.Empty(typeof(CrossChannelGenerator).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic));
    }

    /// <summary>
    /// Asserts that the generator reports nothing, and that the output has no error and no warning located in the generated code.
    /// </summary>
    private static void AssertCompilesCleanly((Compilation Output, Diagnostic[] Diagnostics, SyntaxTree[] GeneratedTrees) result)
    {
        Assert.Empty(result.Diagnostics.Where(x => x.Severity >= DiagnosticSeverity.Warning).Select(x => x.ToString()));
        Assert.Empty(result.Output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(x => x.Severity == DiagnosticSeverity.Error ||
                (x.Severity == DiagnosticSeverity.Warning && Array.IndexOf(result.GeneratedTrees, x.Location.SourceTree) >= 0))
            .Select(x => x.ToString()));
    }

    private static (Compilation Output, Diagnostic[] Diagnostics, SyntaxTree[] GeneratedTrees) Run(string source, string path = "", MetadataReference[]? references = null, DocumentationMode documentationMode = DocumentationMode.Parse, bool allowUnsafe = false)
    {
        var parseOptions = CSharpParseOptions.Default.WithDocumentationMode(documentationMode);
        var compilation = CSharpCompilation.Create("GeneratorInput",
            new[] { CSharpSyntaxTree.ParseText(source, parseOptions, path) }, references ?? References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: allowUnsafe));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new CrossChannelGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        return (output, diagnostics.ToArray(), driver.GetRunResult().GeneratedTrees.ToArray());
    }
}
