// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Immutable;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable RS1036

namespace CrossChannel.Generator;

[Generator]
public class CrossChannelGeneratorV2 : IIncrementalGenerator, IGeneratorInformation
{
    public bool AttachDebugger { get; private set; }

    public bool GenerateToFile { get; private set; }

    public string? CustomNamespace { get; private set; }

    public string? AssemblyName { get; private set; }

    public int AssemblyId { get; private set; }

    public OutputKind OutputKind { get; private set; }

    public string? TargetFolder { get; private set; }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.CompilationProvider.Combine(
            context.SyntaxProvider
            .CreateSyntaxProvider(static (s, _) => IsSyntaxTargetForGeneration(s), static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Collect());

        context.RegisterImplementationSourceOutput(provider, this.Emit);
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
        node is TypeDeclarationSyntax m && m.AttributeLists.Count > 0; // m.BaseList?.Types.Count > 0;

    private static TypeDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var typeSyntax = (TypeDeclarationSyntax)context.Node;
        foreach (var attributeList in typeSyntax.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name.EndsWith(CrossChannelGeneratorOptionAttributeMock.StandardName) ||
                    name.EndsWith(CrossChannelGeneratorOptionAttributeMock.SimpleName))
                {
                    return typeSyntax;
                }
                else if (name.EndsWith(NetServiceObjectAttributeMock.StandardName) ||
                    name.EndsWith(NetServiceObjectAttributeMock.SimpleName))
                {
                    return typeSyntax;
                }
                else if (name.EndsWith(NetServiceInterfaceAttributeMock.StandardName) ||
                    name.EndsWith(NetServiceInterfaceAttributeMock.SimpleName))
                {
                    return typeSyntax;
                }
            }
        }

        /*if (typeSyntax.BaseList is { } baseList)
        {
            foreach (var baseType in baseList.Types)
            {
                if (baseType.ToString() == INetService.StandardName)
                {
                    return typeSyntax;
                }
            }
        }*/

        return null;
    }

    private void Emit(SourceProductionContext context, (Compilation Compilation, ImmutableArray<TypeDeclarationSyntax?> Types) source)
    {
        var compilation = source.Compilation;

        var netServiceObjectAttributeSymbol = compilation.GetTypeByMetadataName(NetServiceObjectAttributeMock.FullName);
        if (netServiceObjectAttributeSymbol == null)
        {
            return;
        }

        var netServiceInterfaceAttributeSymbol = compilation.GetTypeByMetadataName(NetServiceInterfaceAttributeMock.FullName);
        if (netServiceInterfaceAttributeSymbol == null)
        {
            return;
        }

        var netServiceInterfaceSymbol = compilation.GetTypeByMetadataName(INetService.FullName);
        if (netServiceInterfaceSymbol == null)
        {
            return;
        }

        var netsphereGeneratorOptionAttributeSymbol = compilation.GetTypeByMetadataName(CrossChannelGeneratorOptionAttributeMock.FullName);
        if (netsphereGeneratorOptionAttributeSymbol == null)
        {
            return;
        }

        this.AssemblyName = compilation.AssemblyName ?? string.Empty;
        this.AssemblyId = this.AssemblyName.GetHashCode();
        this.OutputKind = compilation.Options.OutputKind;

        var body = new CrossChannelBody(context);
#pragma warning disable RS1024 // Symbols should be compared for equality
        var processed = new HashSet<INamedTypeSymbol?>();
#pragma warning restore RS1024 // Symbols should be compared for equality

        var generatorOptionSet = false;
        foreach (var x in source.Types)
        {
            if (x == null)
            {
                continue;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            var model = compilation.GetSemanticModel(x.SyntaxTree);
            if (model.GetDeclaredSymbol(x) is INamedTypeSymbol s &&
                !processed.Contains(s))
            {
                processed.Add(s);

                /*if (s.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(netServiceInterfaceSymbol, x)))
                {
                    body.Add(s);
                    break;
                }*/

                foreach (var y in s.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(y.AttributeClass, netServiceObjectAttributeSymbol))
                    { // NetServiceObject
                        body.Add(s);
                        break;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(y.AttributeClass, netServiceInterfaceAttributeSymbol))
                    { // NetServiceInterface
                        body.Add(s);
                        break;
                    }
                    else if (!generatorOptionSet &&
                        SymbolEqualityComparer.Default.Equals(y.AttributeClass, netsphereGeneratorOptionAttributeSymbol))
                    {// CrossChannelGeneratorOption
                        generatorOptionSet = true;
                        var va = new VisceralAttribute(CrossChannelGeneratorOptionAttributeMock.FullName, y);
                        var ta = CrossChannelGeneratorOptionAttributeMock.FromArray(va.ConstructorArguments, va.NamedArguments);

                        this.AttachDebugger = ta.AttachDebugger;
                        this.GenerateToFile = ta.GenerateToFile;
                        this.CustomNamespace = ta.CustomNamespace;
                        this.TargetFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(x.SyntaxTree.FilePath), "Generated");
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
        body.Generate(this, context.CancellationToken);
    }
}
