using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MathMax.Generators.ChangeTracking;

#pragma warning disable S1192 // Enable analyzer release tracking

[Generator]
public class ChangeTrackerCodeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Build minimal pipeline: collect invocation nodes then delegate heavy logic.
        var invocations = CreateInvocationProvider(context);
        var collected = invocations.Collect();
        context.RegisterSourceOutput(context.CompilationProvider.Combine(collected), static (spc, tuple) =>
            ProcessCompilation(spc, tuple.Left, tuple.Right));
    }

    // --- Initialization helpers (extracted to reduce cognitive complexity in Initialize) ---
    private static IncrementalValuesProvider<InvocationExpressionSyntax> CreateInvocationProvider(IncrementalGeneratorInitializationContext context) =>
        context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (s, _) => s is InvocationExpressionSyntax,
            transform: static (ctx, _) => (InvocationExpressionSyntax)ctx.Node);

    private static void ProcessCompilation(SourceProductionContext spc, Compilation compilation, ImmutableArray<InvocationExpressionSyntax> invocationNodes)
    {
        if (invocationNodes.IsDefaultOrEmpty)
            return;

        if (!TryGetDslTypes(compilation, out var changeTrackingClass, out _))
            return; // DSL types not present

        var (perRoot, rootNamespaces) = ExtractRootTracks(compilation, invocationNodes, changeTrackingClass);
        EmitSources(spc, perRoot, rootNamespaces);
    }

    private static bool TryGetDslTypes(Compilation compilation, out INamedTypeSymbol changeTrackingClass, out INamedTypeSymbol trackByExtensions)
    {
        changeTrackingClass = compilation.GetTypeByMetadataName("MathMax.ChangeTracking.ChangeTracking")!;
        trackByExtensions = compilation.GetTypeByMetadataName("MathMax.ChangeTracking.TrackByExtensions")!;
        return changeTrackingClass is not null && trackByExtensions is not null;
    }

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

    private static bool TryGetMapInvocationRoot(Compilation compilation, InvocationExpressionSyntax invocation, INamedTypeSymbol changeTrackingClass, out INamedTypeSymbol rootType)
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

    private static SyntaxNode? GetLambdaBody(InvocationExpressionSyntax invocation)
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

    private static void EmitSources(SourceProductionContext spc, Dictionary<INamedTypeSymbol, List<TrackInfo>> perRoot, Dictionary<INamedTypeSymbol, string> rootNamespaces)
    {
        foreach (var kvp in perRoot)
        {
            var entityType = kvp.Key;
            var tracks = kvp.Value
                .GroupBy(t => (t.OwnerType, t.CollectionPropertyName))
                .Select(g => g.First())
                .ToList();
            rootNamespaces.TryGetValue(entityType, out var ns);
            var source = GenerateSource(entityType, tracks, ns);
            spc.AddSource(entityType.Name + ".ChangeTracking.g.cs", source);
        }
    }

    private sealed class TrackInfo
    {
        public INamedTypeSymbol OwnerType { get; }
        public string CollectionPropertyName { get; }
        public INamedTypeSymbol ElementType { get; }
        public string KeySelectorExpression { get; }
        public TrackInfo(INamedTypeSymbol ownerType, string prop, INamedTypeSymbol elementType, string keySelectorExpression)
        {
            OwnerType = ownerType;
            CollectionPropertyName = prop;
            ElementType = elementType;
            KeySelectorExpression = keySelectorExpression;
        }
    }
    private static void CollectTrackByInvocations(Compilation compilation, INamedTypeSymbol rootType, SyntaxNode body,
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

    private static bool TryCreateTrackInfo(Compilation compilation, InvocationExpressionSyntax invocation, out TrackInfo info)
    {
        info = null!; // will be set on success
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        if (!IsTrackByInvocation(compilation, invocation, memberAccess))
        {
            return false;
        }

        if (memberAccess.Expression is not MemberAccessExpressionSyntax propAccess)
        {
            return false; // expecting something like param.Property.TrackBy
        }

        var propertySymbol = GetPropertySymbol(compilation, propAccess);
        if (propertySymbol is null)
        {
            return false;
        }

        if (GetElementType(propertySymbol.Type) is not INamedTypeSymbol elementTypeSymbol)
        {
            return false;
        }

        var args = invocation.ArgumentList?.Arguments;
        if (args is null || args.Value.Count == 0)
        {
            return false;
        }

        if (args.Value[0].Expression is not LambdaExpressionSyntax keyLambda)
        {
            return false; // enforce simple lambda
        }

        var keySelectorText = keyLambda.ToString();
        info = new TrackInfo(propertySymbol.ContainingType, propertySymbol.Name, elementTypeSymbol, keySelectorText);
        return true;
    }

    private static bool IsTrackByInvocation(Compilation compilation, InvocationExpressionSyntax invocation, MemberAccessExpressionSyntax memberAccess)
    {
        if (!string.Equals(memberAccess.Name.Identifier.Text, "TrackBy", StringComparison.Ordinal))
        {
            return false;
        }

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
        if (collectionType is null)
        {
            return null;
        }
        if (collectionType is IArrayTypeSymbol ats)
        {
            return ats.ElementType;
        }
        if (collectionType is INamedTypeSymbol named && named.TypeArguments.Length == 1 &&
            (named.AllInterfaces.Any(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T) ||
             named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T))
        {
            return named.TypeArguments[0];
        }
        // Try any implemented IEnumerable<T>
        foreach (var interfaceSymbol in collectionType.AllInterfaces)
        {
            if (interfaceSymbol.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T && interfaceSymbol.TypeArguments.Length == 1)
            {
                return interfaceSymbol.TypeArguments[0];
            }
        }
        return null;
    }

    private static string GenerateSource(INamedTypeSymbol entityType, List<TrackInfo> tracks, string? namespaceName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using MathMax.ChangeTracking;");
        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            sb.Append("namespace ").Append(namespaceName).AppendLine(";");
            sb.AppendLine();
        }

        var className = entityType.Name + "ChangeTrackerGenerated";
        sb.Append("public static class ").Append(className).AppendLine();
        sb.AppendLine("{");

    // Build full set of involved types (entity + all element types + complex property graph)
    var involvedTypes = DiscoverInvolvedTypes(entityType, tracks);

        // Order: root first, then others deterministically by name
        var orderedTypes = involvedTypes.OrderBy(t => t.Equals(entityType, SymbolEqualityComparer.Default) ? 0 : 1)
            .ThenBy(t => t.Name).ToList();

        // Generate GetDifferences for root (public)
    GenerateGetDifferencesMethod(sb, entityType, tracks, isRoot: true);

        // Generate for others (private)
        foreach (var t in orderedTypes.Where(t => !SymbolEqualityComparer.Default.Equals(t, entityType)))
        {
            GenerateGetDifferencesMethod(sb, t, tracks, isRoot: false);
        }

        sb.AppendLine("}"); // end class
        return sb.ToString();
    }

    private static void GenerateGetDifferencesMethod(StringBuilder sb, INamedTypeSymbol currentType, List<TrackInfo> allTracks, bool isRoot)
    {
        AppendMethodHeader(sb, currentType, isRoot);
        AppendNullChecks(sb);
        sb.AppendLine();
        AppendScalarPropertyDiffs(sb, currentType);
        sb.AppendLine();
        AppendComplexPropertyDiffs(sb, currentType);
        sb.AppendLine();
        AppendCollectionDiffs(sb, currentType, allTracks);
        AppendMethodFooter(sb);
    }

    // --- Helper emission methods (pure string building, no semantic changes) ---
    private static void AppendMethodHeader(StringBuilder sb, INamedTypeSymbol currentType, bool isRoot)
    {
        var accessibility = isRoot ? "public" : "private";
        sb.Append("    ").Append(accessibility).Append(" static IEnumerable<Difference> GetDifferences(")
            .Append("this ")
            .Append(currentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .Append(" left, ")
            .Append(currentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .Append(" right, string path = nameof(")
            .Append(currentType.Name)
            .AppendLine("))")
            .AppendLine("    {");
    }

    private static void AppendNullChecks(StringBuilder sb)
    {
        sb.AppendLine("        if (left == null && right == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            yield break;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (left == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            yield return new Difference { Path = path, LeftOwner = null, RightOwner = null, LeftValue = left, RightValue = right, Kind = DifferenceKind.Addition };");
        sb.AppendLine("            yield break;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (right == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            yield return new Difference { Path = path, LeftOwner = null, RightOwner = null, LeftValue = left, RightValue = right, Kind = DifferenceKind.Removal };");
        sb.AppendLine("            yield break;");
        sb.AppendLine("        }");
    }

    private static void AppendScalarPropertyDiffs(StringBuilder sb, INamedTypeSymbol currentType)
    {
        var scalarPropNames = currentType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => !p.IsIndexer && p.GetMethod is not null && p.Type.IsSimpleType())
            .Select(prop => prop.Name);

        foreach (var propName in scalarPropNames)
        {
            sb.Append("        if (left.").Append(propName).Append(" != right.").Append(propName).AppendLine(")");
            sb.Append("            yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof(")
                .Append(currentType.Name).Append('.').Append(propName)
                .Append("), left, right, left.").Append(propName)
                .Append(", right.").Append(propName).AppendLine(");");
        }
    }

    private static void AppendComplexPropertyDiffs(StringBuilder sb, INamedTypeSymbol currentType)
    {
        var complexPropNames = currentType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => !p.IsIndexer && p.GetMethod is not null && p.Type.GetTypeCategory() == TypeCategory.Complex)
            .Select(prop => prop.Name);

        foreach (var propName in complexPropNames)
        {
            sb.AppendLine();
            // Presence: left null, right non-null (treat as property difference)
            sb.AppendLine($"        if (left.{propName} == null && right.{propName} != null)");
            sb.AppendLine("        {");
            sb.AppendLine($"            yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof({currentType.Name}.{propName}), left, right, null, right.{propName});");
            sb.AppendLine("        }");
            sb.AppendLine();
            // Presence: left non-null, right null (treat as property difference)
            sb.AppendLine($"        if (left.{propName} != null && right.{propName} == null)");
            sb.AppendLine("        {");
            sb.AppendLine($"            yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof({currentType.Name}.{propName}), left, right, left.{propName}, null);");
            sb.AppendLine("        }");
            sb.AppendLine();
            // Both non-null: recurse
            sb.AppendLine($"        if (left.{propName} != null && right.{propName} != null)");
            sb.AppendLine("        {");
            sb.AppendLine($"            foreach (var diff in left.{propName}.GetDifferences(right.{propName}, path + \".{propName}\"))");
            sb.AppendLine("                yield return diff;");
            sb.AppendLine("        }");
        }
    }

    private static void AppendCollectionDiffs(StringBuilder sb, INamedTypeSymbol currentType, List<TrackInfo> allTracks)
    {
        var owningTracks = allTracks.Where(t => SymbolEqualityComparer.Default.Equals(t.OwnerType, currentType));
        foreach (var track in owningTracks)
        {
            var elementType = track.ElementType;
            var propName = track.CollectionPropertyName;
            sb.AppendLine();
            sb.Append("        if (left.").Append(propName).Append(" != null || right.").Append(propName).AppendLine(" != null)");
            sb.AppendLine("        {");
            sb.Append("            var leftList = left.").Append(propName).AppendLine(" ?? System.Array.Empty<" + elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ">();");
            sb.Append("            var rightList = right.").Append(propName).AppendLine(" ?? System.Array.Empty<" + elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ">();");
            sb.AppendLine($"            foreach (var diff in ChangeTrackerExtensions.DiffListByIdentity(leftList, rightList, path + \".{propName}\", (l, r, pth) => l.GetDifferences(r, pth), {track.KeySelectorExpression}))");
            sb.AppendLine("            { yield return diff; }");
            sb.AppendLine("        }");
        }
    }

    private static void AppendMethodFooter(StringBuilder sb)
    {
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static HashSet<INamedTypeSymbol> DiscoverInvolvedTypes(INamedTypeSymbol root, List<TrackInfo> tracks)
    {
        var set = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default) { root };
        var queue = new Queue<INamedTypeSymbol>();
        void Enqueue(INamedTypeSymbol t)
        {
            if (set.Add(t))
            {
                queue.Enqueue(t);
            }
        }
        // Ensure root is traversed for complex property discovery
        queue.Enqueue(root);
        foreach (var e in tracks.Select(t => t.ElementType))
        {
            Enqueue(e);
        }

        foreach (var o in tracks.Select(t => t.OwnerType))
        {
            Enqueue(o);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var typeSymbol in current.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsIndexer).Select(property => property.Type))
            {
                var cat = typeSymbol.GetTypeCategory();
                if (cat == TypeCategory.Complex && typeSymbol is INamedTypeSymbol nts)
                {
                    Enqueue(nts);
                }
            }
        }
        return set;
    }

    private static string GetEnclosingNamespace(SyntaxNode node)
    {
        // Walk up until we find a namespace declaration; if none, return empty string.
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
}

#pragma warning restore S1192 // Enable analyzer release tracking