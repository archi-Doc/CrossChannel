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
    public const string GeneratorName = "CrossChannelGenerator";
    public const string FrontendClassName = "Frontend_"; // "__gen_frontend__";
    public const string BackendClassName = "Backend_";
    public const string ArgumentName = "a";
    public const string NetResultFullName = "CrossChannel.NetResult";
    // public const string NetServiceBaseFullName = "CrossChannel.NetServiceBase";
    // public const string NetServiceBaseFullName2 = "CrossChannel.NetServiceBase<TServerContext>";
    public const string ServiceFilterSyncFullName = "CrossChannel.IServiceFilterSync";
    public const string ServiceFilterSyncFullName2 = "CrossChannel.IServiceFilterSync<TCallContext>";
    public const string ServiceFilterAsyncFullName = "CrossChannel.IServiceFilter";
    public const string ServiceFilterAsyncFullName2 = "CrossChannel.IServiceFilter<TCallContext>";
    public const string ServiceFilterBaseName = "IServiceFilterBase";
    public const string ServiceFilterInvokeName = "Invoke";
    public const string ServiceFilterSetArgumentsName = "SetArguments";
    public const string IClientConnectionInternalName = "CrossChannel.Internal.IClientConnectionInternal";

    public static readonly DiagnosticDescriptor Error_AttributePropertyError = new DiagnosticDescriptor(
        id: "NSG001", title: "Attribute property type error", messageFormat: "The argument specified does not match the type of the property",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_IRadioService = new DiagnosticDescriptor(
        id: "NSG002", title: "IRadioService", messageFormat: "RadioServiceInterface must be derived from IRadioService",
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
        var assemblyId = string.Empty; // Assembly ID
        if (!string.IsNullOrEmpty(generator.AssemblyName))
        {
            assemblyId = VisceralHelper.AssemblyNameToIdentifier("_" + generator.AssemblyName);
        }

        this.GenerateLoader(generator, assemblyId);
        this.GenerateBroker(generator, assemblyId);
    }

    public void GenerateLoader(IGeneratorInformation generator, string assemblyId)
    {
        ScopingStringBuilder ssb = new();

        var array = this.IdToNetInterface.Values.ToArray();

        this.GenerateHeader(ssb);
        ssb.AppendLine($"namespace CrossChannel.Generated;");
        ssb.AppendLine();

        using (var scopeClass = ssb.ScopeBrace("internal static class Frontend" + assemblyId))
        {
            ssb.AppendLine("private static bool Initialized;");
            ssb.AppendLine();
            ssb.AppendLine("[ModuleInitializer]");

            using (var scopeMethod = ssb.ScopeBrace("public static void Initialize()"))
            {
                ssb.AppendLine("if (Initialized) return;");
                ssb.AppendLine("Initialized = true;");
                ssb.AppendLine();

                foreach (var y in array.Where(a => a.ObjectFlag.HasFlag(CrossChannelObjectFlag.NetServiceInterface)))
                {
                    ssb.AppendLine($"StaticNetService.SetFrontendDelegate<{y.FullName}>(static x => new {y.ClassName}(x));");
                }
            }

            foreach (var y in array)
            {
                if (y.ObjectFlag.HasFlag(CrossChannelObjectFlag.NetServiceInterface))
                {// NetServiceInterface (Frontend)
                    ssb.AppendLine();
                    y.GenerateFrontend(ssb, info);
                }
            }

            var result = ssb.Finalize();
            if (generator.GenerateToFile && generator.TargetFolder != null && Directory.Exists(generator.TargetFolder))
            {
                this.StringToFile(result, Path.Combine(generator.TargetFolder, $"gen.CrossChannel.Frontend.cs"));
            }
            else
            {
                this.Context?.AddSource($"gen.CrossChannel.Frontend", SourceText.From(result, Encoding.UTF8));
                this.Context2?.AddSource($"gen.CrossChannel.Frontend", SourceText.From(result, Encoding.UTF8));
            }
        }

        this.FlushDiagnostic();
    }

    public void GenerateBroker(IGeneratorInformation generator, string assemblyId)
    {
        ScopingStringBuilder ssb = new();

        var array = this.Objects.ToArray();

        this.GenerateHeader(ssb);
        ssb.AppendLine($"namespace CrossChannel.Generated;");
        ssb.AppendLine();

        using (var scopeClass = ssb.ScopeBrace("internal static class Backend" + assemblyId))
        {
            ssb.AppendLine("private static bool Initialized;");
            ssb.AppendLine();
            ssb.AppendLine("[ModuleInitializer]");

            using (var scopeMethod = ssb.ScopeBrace("public static void Initialize()"))
            {
                ssb.AppendLine("if (Initialized) return;");
                ssb.AppendLine("Initialized = true;");
                ssb.AppendLine();

                foreach (var y in array.Where(a => a.ObjectFlag.HasFlag(CrossChannelObjectFlag.NetServiceObject)))
                {
                    if (y.ServiceInterfaces != null)
                    {
                        foreach (var z in y.ServiceInterfaces)
                        {
                            ssb.AppendLine($"StaticNetService.SetServiceInfo({y.ClassName}.ServiceInfo_{z.NetServiceInterfaceAttribute!.ServiceId.ToString("x")}());");
                        }
                    }
                }
            }

            foreach (var y in array)
            {
                if (y.ObjectFlag.HasFlag(CrossChannelObjectFlag.NetServiceObject))
                {// NetServiceObject (Backend)
                    ssb.AppendLine();
                    y.GenerateBackend(ssb, info);
                }
            }

            var result = ssb.Finalize();
            if (generator.GenerateToFile && generator.TargetFolder != null && Directory.Exists(generator.TargetFolder))
            {
                this.StringToFile(result, Path.Combine(generator.TargetFolder, $"gen.CrossChannel.Backend.cs"));
            }
            else
            {
                this.Context?.AddSource($"gen.CrossChannel.Backend", SourceText.From(result, Encoding.UTF8));
                this.Context2?.AddSource($"gen.CrossChannel.Backend", SourceText.From(result, Encoding.UTF8));
            }
        }

        this.FlushDiagnostic();
    }

    private void GenerateHeader(ScopingStringBuilder ssb)
    {
        ssb.AddHeader("// <auto-generated/>");
        ssb.AddUsing("System");
        ssb.AddUsing("System.Collections.Generic");
        ssb.AddUsing("System.Diagnostics.CodeAnalysis");
        ssb.AddUsing("System.Runtime.CompilerServices");
        // ssb.AddUsing("Arc.Collections");
        ssb.AddUsing("CrossChannel");

        ssb.AppendLine("#nullable enable", false);
        ssb.AppendLine("#pragma warning disable CS1591", false);
        ssb.AppendLine("#pragma warning disable CS1998", false);
        ssb.AppendLine();
    }

    private void GenerateInitializer(IGeneratorInformation generator, ScopingStringBuilder ssb)
    {
        // Namespace
        var ns = "CrossChannel";
        var assemblyId = string.Empty; // Assembly ID
        if (!string.IsNullOrEmpty(generator.CustomNamespace))
        {// Custom namespace.
            ns = generator.CustomNamespace;
        }
        else
        {// Other (Apps)
         // assemblyId = "_" + generator.AssemblyId.ToString("x");
            if (!string.IsNullOrEmpty(generator.AssemblyName))
            {
                assemblyId = VisceralHelper.AssemblyNameToIdentifier("_" + generator.AssemblyName);
            }
        }

        info.ModuleInitializerClass.Add("CrossChannel.Generator.Generated");

        ssb.AppendLine();
        using (var scopeCrossLink = ssb.ScopeNamespace(ns!))
        using (var scopeClass = ssb.ScopeBrace("public static class CrossChannelModule" + assemblyId))
        {
            ssb.AppendLine("private static bool Initialized;");
            ssb.AppendLine();
            ssb.AppendLine("[ModuleInitializer]");

            using (var scopeMethod = ssb.ScopeBrace("public static void Initialize()"))
            {
                ssb.AppendLine("if (Initialized) return;");
                ssb.AppendLine("Initialized = true;");
                ssb.AppendLine();

                foreach (var x in info.ModuleInitializerClass)
                {
                    ssb.Append(x, true);
                    ssb.AppendLine(".RegisterBM();", false);
                }
            }
        }
    }
}
