// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Immutable;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable RS1036

namespace CrossChannel.Generator;

[Generator]
public class CrossChannelGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        /*if (syntax.BaseList is not null)
        {// IRadioService (check later)
            foreach (var baseType in syntax.BaseList.Types)
            {
                var name = baseType.ToString();
                if (name.EndsWith(IRadioServiceMock.StandardName))
                {
                    return syntax;
                }
            }
        }*/

        // [RadioService] interfaces
        var services = context.SyntaxProvider.ForAttributeWithMetadataName(
            RadioServiceAttributeMock.FullName,
            static (node, _) => node is InterfaceDeclarationSyntax,
            static (context, _) => (INamedTypeSymbol)context.TargetSymbol)
            .Collect();

        // [CrossChannelGeneratorOptions] and the path of the annotated file
        var options = context.SyntaxProvider.ForAttributeWithMetadataName(
            CrossChannelGeneratorOptionsAttributeMock.FullName,
            static (node, _) => node is InterfaceDeclarationSyntax,
            static (context, _) => (Attribute: context.Attributes[0], FilePath: context.TargetNode.SyntaxTree.FilePath))
            .Collect();

        var provider = context.CompilationProvider.Combine(services).Combine(options);
        context.RegisterImplementationSourceOutput(provider, static (context, source) => Emit(context, source.Left.Left, source.Left.Right, source.Right));
    }

    private static void Emit(SourceProductionContext context, Compilation compilation, ImmutableArray<INamedTypeSymbol> services, ImmutableArray<(AttributeData Attribute, string FilePath)> options)
    {
        if (services.IsDefaultOrEmpty)
        {
            return;
        }

        var iRadioService = compilation.GetTypeByMetadataName(IRadioServiceMock.FullName);
        if (iRadioService == null)
        {
            return;
        }

        // The host may share a generator instance between compilations, so the options of this run are kept locally.
        var information = new GeneratorInformation(compilation.AssemblyName ?? string.Empty, compilation.Options.OutputKind);
        if (options.Length > 0)
        {// [CrossChannelGeneratorOptions] (the first one takes effect)
            var (y, filePath) = options[0];
            var attribute = new VisceralAttribute(CrossChannelGeneratorOptionsAttributeMock.FullName, y);
            var generatorOption = CrossChannelGeneratorOptionsAttributeMock.FromArray(attribute.ConstructorArguments, attribute.NamedArguments);

            information.AttachDebugger = generatorOption.AttachDebugger;
            information.GenerateToFile = generatorOption.GenerateToFile;

            // A syntax tree which is not backed by a file (e.g. an in-memory compilation) has no folder to write to.
            var directory = string.IsNullOrEmpty(filePath) ? null : Path.GetDirectoryName(filePath);
            information.TargetFolder = string.IsNullOrEmpty(directory) ? null : Path.Combine(directory, "Generated");
        }

        var body = new CrossChannelBody(context);
        foreach (var symbol in services)
        {// [RadioService] (Body.Add ignores a symbol which is already added)
            context.CancellationToken.ThrowIfCancellationRequested();
            body.Add(symbol);
            // if (symbol.AllInterfaces.Any(z => SymbolEqualityComparer.Default.Equals(z, iRadioService))) // IRadioService (check later)
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        body.Prepare();
        if (body.Abort)
        {
            return;
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        body.Generate(information, context.CancellationToken);
    }

    private sealed class GeneratorInformation : IGeneratorInformation
    {
        public GeneratorInformation(string assemblyName, OutputKind outputKind)
        {
            this.AssemblyName = assemblyName;
            this.OutputKind = outputKind;
        }

        public bool AttachDebugger { get; set; }

        public bool GenerateToFile { get; set; }

        public string? CustomNamespace => null;

        public string? AssemblyName { get; }

        public int AssemblyId => 0; // Not used.

        public OutputKind OutputKind { get; }

        public string? TargetFolder { get; set; }
    }
}
