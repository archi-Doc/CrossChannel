// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Immutable;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable RS1036

namespace CrossChannel.Generator;

[Generator]
public class CrossChannelGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.CompilationProvider.Combine(
            context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) =>
                {// Interface declaration with one or more attributes.
                    return node is InterfaceDeclarationSyntax syntax && syntax.AttributeLists.Count > 0;
                },
                static (context, _) =>
                {
                    if (context.Node is InterfaceDeclarationSyntax syntax)
                    {
                        foreach (var attributeList in syntax.AttributeLists)
                        {
                            foreach (var attribute in attributeList.Attributes)
                            {
                                var name = context.SemanticModel.GetSymbolInfo(attribute).Symbol?.ContainingType.ToDisplayString();
                                if (name == CrossChannelGeneratorOptionsAttributeMock.FullName)
                                {// [CrossChannelGeneratorOptionsAttribute]
                                    return syntax;
                                }
                                else if (name == RadioServiceAttributeMock.FullName)
                                {// [RadioServiceAttribute]
                                    return syntax;
                                }
                            }
                        }

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
                    }

                    return null;
                })
            .Collect());

        context.RegisterImplementationSourceOutput(provider, Emit);
    }

    private static void Emit(SourceProductionContext context, (Compilation Compilation, ImmutableArray<InterfaceDeclarationSyntax?> Types) source)
    {
        var compilation = source.Compilation;

        var generatorOptionAttributeSymbol = compilation.GetTypeByMetadataName(CrossChannelGeneratorOptionsAttributeMock.FullName);
        if (generatorOptionAttributeSymbol == null)
        {
            return;
        }

        var iRadioService = compilation.GetTypeByMetadataName(IRadioServiceMock.FullName);
        if (iRadioService == null)
        {
            return;
        }

        var radioServiceInterface = compilation.GetTypeByMetadataName(RadioServiceAttributeMock.FullName);
        if (radioServiceInterface == null)
        {
            return;
        }

        // The host may share a generator instance between compilations, so the options of this run are kept locally.
        var information = new GeneratorInformation(compilation.AssemblyName ?? string.Empty, compilation.Options.OutputKind);
        var body = new CrossChannelBody(context);
        var processed = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        var generatorOptionIsSet = false;
        foreach (var x in source.Types)
        {
            if (x == null)
            {
                continue;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            var model = compilation.GetSemanticModel(x.SyntaxTree);
            if (model.GetDeclaredSymbol(x) is INamedTypeSymbol symbol &&
                symbol.TypeKind == TypeKind.Interface &&
                processed.Add(symbol))
            {
                foreach (var y in symbol.GetAttributes())
                {
                    if (!generatorOptionIsSet &&
                        SymbolEqualityComparer.Default.Equals(y.AttributeClass, generatorOptionAttributeSymbol))
                    {// [CrossChannelGeneratorOptions]
                        generatorOptionIsSet = true;
                        var attribute = new VisceralAttribute(CrossChannelGeneratorOptionsAttributeMock.FullName, y);
                        var generatorOption = CrossChannelGeneratorOptionsAttributeMock.FromArray(attribute.ConstructorArguments, attribute.NamedArguments);

                        information.AttachDebugger = generatorOption.AttachDebugger;
                        information.GenerateToFile = generatorOption.GenerateToFile;

                        // A syntax tree which is not backed by a file (e.g. an in-memory compilation) has no folder to write to.
                        var filePath = x.SyntaxTree.FilePath;
                        var directory = string.IsNullOrEmpty(filePath) ? null : Path.GetDirectoryName(filePath);
                        information.TargetFolder = string.IsNullOrEmpty(directory) ? null : Path.Combine(directory, "Generated");
                    }
                    else if (SymbolEqualityComparer.Default.Equals(y.AttributeClass, radioServiceInterface))
                    {// [RadioService]
                        body.Add(symbol);
                        // if (symbol.AllInterfaces.Any(z => SymbolEqualityComparer.Default.Equals(z, iRadioService))) // IRadioService (check later)
                    }
                }
            }
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
            this.AssemblyId = assemblyName.GetHashCode();
            this.OutputKind = outputKind;
        }

        public bool AttachDebugger { get; set; }

        public bool GenerateToFile { get; set; }

        public string? CustomNamespace => null;

        public string? AssemblyName { get; }

        public int AssemblyId { get; }

        public OutputKind OutputKind { get; }

        public string? TargetFolder { get; set; }
    }
}
