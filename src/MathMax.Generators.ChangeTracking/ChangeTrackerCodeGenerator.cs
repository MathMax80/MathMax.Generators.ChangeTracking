using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MathMax.Generators.ChangeTracking;

[Generator]
public partial class ChangeTrackerCodeGenerator : IIncrementalGenerator
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

        // Build model (parsing responsibility)
        var model = TrackModelBuilder.Build(compilation, invocationNodes, changeTrackingClass);
        if (model.TracksPerRoot.Count == 0)
            return;
        // Emit sources (emission responsibility)
        SourceEmitter.Emit(spc, model);
    }

    private static bool TryGetDslTypes(Compilation compilation, out INamedTypeSymbol changeTrackingClass, out INamedTypeSymbol trackByExtensions)
    {
        changeTrackingClass = compilation.GetTypeByMetadataName("MathMax.ChangeTracking.ChangeTracking")!;
        trackByExtensions = compilation.GetTypeByMetadataName("MathMax.ChangeTracking.TrackByExtensions")!;
        return changeTrackingClass is not null && trackByExtensions is not null;
    }
}
