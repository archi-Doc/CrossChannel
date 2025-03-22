// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using System.Threading;
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

    public static readonly DiagnosticDescriptor Error_NotPartialParent = new DiagnosticDescriptor(
        id: "CCG001", title: "Partial class/struct", messageFormat: "Parent type '{0}' is not a partial class/struct",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_IRadioService = new DiagnosticDescriptor(
        id: "CCG002", title: "IRadioService", messageFormat: "Types with the RadioServiceInterface attribute must derive from IRadioService",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_MethodReturnType = new DiagnosticDescriptor(
        id: "CCG003", title: "Method return type", messageFormat: "The return type of the method must be void, Task, RadioResult<T>, Task<RadioResult<T>>",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public CrossChannelBody(SourceProductionContext context)
        : base(context)
    {
    }

    internal List<CrossChannelObject> Objects = new();

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

        // Namespace: string - List<CrossChannelObject>
        foreach (var x in this.Namespaces)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.GenerateHeader(ssb);
            ssb.AppendNamespace(x.Key);

            // var topObjects = x.Value.Where(y => y.ContainingObject is null).ToList();
            // var nestedObjects = x.Value.Where(y => y.ContainingObject is not null).ToList();
            var assemblyId = string.Empty;
            if (!string.IsNullOrEmpty(generator.AssemblyName))
            {
                assemblyId = VisceralHelper.AssemblyNameToIdentifier(generator.AssemblyName!);
            }

            var rootName = $"{RootName}"; // {assemblyId}
            using (var scopeClass = ssb.ScopeBrace($"public static class {rootName}"))
            {
                ssb.AppendLine("private static bool Initialized;");
                ssb.AppendLine("[ModuleInitializer]");

                using (var scopeMethod = ssb.ScopeBrace("public static void Initialize()"))
                {
                    ssb.AppendLine("if (Initialized) return;");
                    ssb.AppendLine("Initialized = true;");

                    foreach (var y in x.Value)
                    {
                        y.GenerateRegister(ssb, false);
                    }

                    foreach (var y in x.Value.Where(y => y.Kind == VisceralObjectKind.Class))
                    {
                        ssb.AppendLine($"{y.FullName}.{InitializerName}();");
                    }
                }

                ssb.AppendLine();
                foreach (var y in x.Value.Where(y => y.Kind != VisceralObjectKind.Class))
                {
                    y.GenerateObject(ssb);
                }
            }

            foreach (var y in x.Value.Where(y => y.Kind == VisceralObjectKind.Class))
            {
                y.GenerateObject(ssb);
            }

            var result = ssb.Finalize();

            if (generator.GenerateToFile && generator.TargetFolder != null && Directory.Exists(generator.TargetFolder))
            {
                this.StringToFile(result, Path.Combine(generator.TargetFolder, $"gen.{Name}.{x.Key}.cs"));
            }
            else
            {
                this.Context?.AddSource($"gen.{Name}.{x.Key}", SourceText.From(result, Encoding.UTF8));
                this.Context2?.AddSource($"gen.{Name}.{x.Key}", SourceText.From(result, Encoding.UTF8));
            }
        }
    }

    private void GenerateHeader(ScopingStringBuilder ssb)
    {
        ssb.AddHeader("// <auto-generated/>");
        ssb.AddUsing("System");
        ssb.AddUsing("System.Collections.Generic");
        ssb.AddUsing("System.Diagnostics.CodeAnalysis");
        ssb.AddUsing("System.Linq");
        ssb.AddUsing("System.Runtime.CompilerServices");
        ssb.AddUsing("System.Threading.Tasks");
        ssb.AddUsing("CrossChannel");

        ssb.AppendLine("#nullable enable", false);
        ssb.AppendLine("#pragma warning disable CS1591", false);
        ssb.AppendLine("#pragma warning disable CS1998", false);
        ssb.AppendLine();
    }
}
