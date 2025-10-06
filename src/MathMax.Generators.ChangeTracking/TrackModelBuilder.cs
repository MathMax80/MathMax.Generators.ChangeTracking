using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MathMax.Generators.ChangeTracking;

/// <summary>
/// Responsible for translating syntax + symbols into a <see cref="TrackGenerationModel"/>.
/// SRP: keeps parsing concern separate from emission.
/// </summary>
public static class TrackModelBuilder
{
    public static TrackGenerationModel Build(Compilation compilation, ImmutableArray<InvocationExpressionSyntax> invocationNodes, INamedTypeSymbol changeTrackingClass)
    {
        var (perRoot, rootNamespaces) = ExtractRootTracks(compilation, invocationNodes, changeTrackingClass);
        return new TrackGenerationModel(perRoot, rootNamespaces);
    }

    // Localized former top-level method: builds dictionaries of tracks + namespaces.
    private static (Dictionary<INamedTypeSymbol, List<TrackInfo>> perRoot, Dictionary<INamedTypeSymbol, string> rootNamespaces)
        ExtractRootTracks(Compilation compilation, ImmutableArray<InvocationExpressionSyntax> invocationNodes, INamedTypeSymbol changeTrackingClass)
    {
        var perRoot = new Dictionary<INamedTypeSymbol, List<TrackInfo>>(SymbolEqualityComparer.Default);
        var rootNamespaces = new Dictionary<INamedTypeSymbol, string>(SymbolEqualityComparer.Default);

        foreach (var invocation in invocationNodes.Distinct())
        {
            if (!TryGetMapInvocationRoot(compilation, invocation, changeTrackingClass, out var rootType))
                continue;

            rootNamespaces[rootType] = GetEnclosingNamespace(invocation);
            var body = GetLambdaBody(invocation);
            if (body == null)
                continue;

            CollectTrackByInvocations(compilation, rootType, body, perRoot);
        }

        return (perRoot, rootNamespaces);
    }

    internal static bool TryGetMapInvocationRoot(Compilation compilation, InvocationExpressionSyntax invocation, INamedTypeSymbol changeTrackingClass, out INamedTypeSymbol rootType)
    {
        rootType = null!;
        var model = compilation.GetSemanticModel(invocation.SyntaxTree);
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
            return false;
        if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, changeTrackingClass))
            return false;
        if (!string.Equals(methodSymbol.Name, "Map", StringComparison.Ordinal))
            return false;
        if (methodSymbol.TypeArguments.Length != 1)
            return false;
        if (methodSymbol.TypeArguments[0] is not INamedTypeSymbol named)
            return false;
        rootType = named;
        return true;
    }

    internal static SyntaxNode? GetLambdaBody(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
            return null;
        if (invocation.ArgumentList.Arguments[0].Expression is not LambdaExpressionSyntax lambda)
            return null;
        return lambda.Body switch
        {
            BlockSyntax b => b,
            ExpressionSyntax e => e,
            _ => null
        };
    }

    internal static void CollectTrackByInvocations(Compilation compilation, INamedTypeSymbol rootType, SyntaxNode body,
        Dictionary<INamedTypeSymbol, List<TrackInfo>> perRoot)
    {
        foreach (var invocation in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (!TryCreateTrackInfo(compilation, invocation, out var info))
            {
                continue;
            }

            if (!perRoot.TryGetValue(rootType, out var list))
            {
                list = [];
                perRoot[rootType] = list;
            }
            list.Add(info);
        }
    }

    internal static string GetEnclosingNamespace(SyntaxNode node)
    {
        for (var current = node; current != null; current = current.Parent)
        {
            switch (current)
            {
                case FileScopedNamespaceDeclarationSyntax fileNs:
                    return fileNs.Name.ToString();
                case NamespaceDeclarationSyntax ns:
                    return ns.Name.ToString();
            }
        }
        return string.Empty;
    }

    // --- Symbol analysis helpers (parsing responsibility) ---
    private static bool TryCreateTrackInfo(Compilation compilation, InvocationExpressionSyntax invocation, out TrackInfo info)
    {
        info = null!; // set on success
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;
        if (!IsTrackByInvocation(compilation, invocation, memberAccess))
            return false;
        if (memberAccess.Expression is not MemberAccessExpressionSyntax propAccess)
            return false; // expecting param.Property.TrackBy

        var propertySymbol = GetPropertySymbol(compilation, propAccess);
        if (propertySymbol is null)
            return false;
        if (GetElementType(propertySymbol.Type) is not INamedTypeSymbol elementTypeSymbol)
            return false;
        var args = invocation.ArgumentList?.Arguments;
        if (args is null || args.Value.Count == 0)
            return false;
        if (args.Value[0].Expression is not LambdaExpressionSyntax keyLambda)
            return false;
        var keySelectorText = keyLambda.ToString();
        info = new TrackInfo(propertySymbol.ContainingType, propertySymbol.Name, elementTypeSymbol, keySelectorText);
        return true;
    }

    private static bool IsTrackByInvocation(Compilation compilation, InvocationExpressionSyntax invocation, MemberAccessExpressionSyntax memberAccess)
    {
        if (!string.Equals(memberAccess.Name.Identifier.Text, "TrackBy", StringComparison.Ordinal))
            return false;
        var model = compilation.GetSemanticModel(invocation.SyntaxTree);
        return model.GetSymbolInfo(invocation).Symbol is IMethodSymbol ms && ms.Name == "TrackBy";
    }

    private static IPropertySymbol? GetPropertySymbol(Compilation compilation, MemberAccessExpressionSyntax propAccess)
    {
        var model = compilation.GetSemanticModel(propAccess.SyntaxTree);
        return model.GetSymbolInfo(propAccess).Symbol as IPropertySymbol;
    }

    private static ITypeSymbol? GetElementType(ITypeSymbol? collectionType)
    {
        if (collectionType is null) return null;
        if (collectionType is IArrayTypeSymbol ats) return ats.ElementType;
        if (collectionType is INamedTypeSymbol named && named.TypeArguments.Length == 1 &&
            (named.AllInterfaces.Any(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T) ||
             named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T))
        {
            return named.TypeArguments[0];
        }
        foreach (var iFace in collectionType.AllInterfaces)
        {
            if (iFace.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T && iFace.TypeArguments.Length == 1)
                return iFace.TypeArguments[0];
        }
        return null;
    }
}
