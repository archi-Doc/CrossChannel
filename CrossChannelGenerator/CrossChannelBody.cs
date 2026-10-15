// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RS2008
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1117 // Parameters should be on same line or separate lines

namespace CrossChannel.Generator;

public class CrossChannelBody : VisceralBody<CrossChannelObject>
{
    public const string Name = "CrossChannel";
    public const string GeneratorName = "CrossChannelGenerator";
    public const string RootName = "__CrossChannelRoot__";
    public const string InitializerName = "__Initialize__";

    /// <summary>
    /// Marks every generated member (initializers and brokers) as obsolete, so that obsolete services, methods,
    /// and types used inside them report no diagnostic: an obsolete context suppresses CS0612, CS0618, CS0619
    /// (Obsolete with error: true), and custom DiagnosticIds, which a pragma cannot fully cover.
    /// </summary>
    public const string ObsoleteContext = "[global::System.Obsolete]";

    public static readonly DiagnosticDescriptor Error_NotPartialParent = new DiagnosticDescriptor(
        id: "CCG001", title: "Partial class/struct", messageFormat: "Parent type '{0}' is not a partial class/struct",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NotRadioService = new DiagnosticDescriptor(
        id: "CCG002", title: "IRadioService", messageFormat: "Types with the RadioService attribute must derive from IRadioService",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_MethodReturnType = new DiagnosticDescriptor(
        id: "CCG003", title: "Method return type", messageFormat: "The return type of the method must be void, Task, RadioResult<T>, Task<RadioResult<T>>",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_UnsupportedMember = new DiagnosticDescriptor(
        id: "CCG004", title: "Unsupported service declaration", messageFormat: "Radio services require non-generic interfaces and instance methods with value parameters; properties, events, generic methods, and ref returns are not supported",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_FileLocal = new DiagnosticDescriptor(
        id: "CCG005", title: "File-local type", messageFormat: "Radio services cannot be file-local or nested in a file-local type '{0}'",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public CrossChannelBody(SourceProductionContext context)
        : base(context)
    {
    }

    internal Dictionary<string, List<CrossChannelObject>> Namespaces = new();

    public void Prepare()
    {
        // Configure objects.
        var array = this.FullNameToObject.Values.ToArray();
        foreach (var x in array)
        {
            x.Configure();
        }

        this.FlushDiagnostic();
        if (this.Abort)
        {
            return;
        }

        foreach (var x in array)
        {
            x.ConfigureRelation();
        }

        // Check
        foreach (var x in array)
        {
            x.Check();
        }

        this.FlushDiagnostic();
        if (this.Abort)
        {
            return;
        }
    }

    public void Generate(IGeneratorInformation generator, CancellationToken cancellationToken)
    {
        var ssb = new ScopingStringBuilder();
        var rootName = string.IsNullOrEmpty(generator.AssemblyName) ? RootName : RootName + VisceralHelper.AssemblyNameToIdentifier(generator.AssemblyName!);
        var hintNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Namespace: string - List<CrossChannelObject>
        foreach (var x in this.Namespaces)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.GenerateHeader(ssb);
            ssb.AppendNamespace(x.Key);

            // var topObjects = x.Value.Where(y => y.ContainingObject is null).ToList();
            // var nestedObjects = x.Value.Where(y => y.ContainingObject is not null).ToList();
            using (var scopeClass = ssb.ScopeBrace($"internal static class {rootName}"))
            {
                ssb.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
                ssb.AppendLine(ObsoleteContext);

                using (var scopeMethod = ssb.ScopeBrace("internal static void Initialize()"))
                {
                    foreach (var y in x.Value)
                    {// Top-level services
                        y.GenerateRegister(ssb);
                    }

                    foreach (var y in x.Value)
                    {// Types which host nested services
                        if (y.IsContainer)
                        {
                            ssb.AppendLine($"{y.QualifiedName}.{InitializerName}();");
                        }
                    }
                }

                ssb.AppendLine();
                foreach (var y in x.Value)
                {// The brokers of top-level services are nested in the root class.
                    y.GenerateOutObject(ssb);
                }
            }

            foreach (var y in x.Value)
            {// The partial declarations of the types which host nested services follow the root class, so that they extend the user's types.
                y.GenerateContainer(ssb);
            }

            var result = ssb.Finalize();

            // Hint names may not contain '@' (escaped keyword namespaces) and must be unique ignoring case.
            var hintName = $"gen.{Name}.{x.Key.Replace("@", string.Empty)}";
            if (!hintNames.Add(hintName))
            {
                var n = 2;
                while (!hintNames.Add($"{hintName}{n}"))
                {
                    n++;
                }

                hintName = $"{hintName}{n}";
            }

            if (generator.GenerateToFile && generator.TargetFolder != null && Directory.Exists(generator.TargetFolder))
            {
                this.StringToFile(result, Path.Combine(generator.TargetFolder, $"{hintName}.cs"));
            }
            else
            {
                this.Context?.AddSource(hintName, SourceText.From(result, Encoding.UTF8));
                this.Context2?.AddSource(hintName, SourceText.From(result, Encoding.UTF8));
            }
        }
    }

    private void GenerateHeader(ScopingStringBuilder ssb)
    {// Every name in the generated code is global:: qualified, so no using directive is needed.
        ssb.AddHeader("// <auto-generated/>");

        ssb.AppendLine("#nullable enable annotations", false); // The signatures keep their annotations, but the generated code reports no nullable warnings (e.g. for [NotNull] or [MaybeNull] parameters).
        ssb.AppendLine();
    }
}
