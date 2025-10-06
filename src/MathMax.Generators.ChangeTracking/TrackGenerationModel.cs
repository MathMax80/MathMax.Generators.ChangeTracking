using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace MathMax.Generators.ChangeTracking;

/// <summary>
/// Encapsulates all data required to generate change tracker sources for discovered root entities.
/// </summary>
public class TrackGenerationModel
{
    public Dictionary<INamedTypeSymbol, List<TrackInfo>> TracksPerRoot { get; }
    public Dictionary<INamedTypeSymbol, string> RootNamespaces { get; }
    public TrackGenerationModel(Dictionary<INamedTypeSymbol, List<TrackInfo>> tracksPerRoot, Dictionary<INamedTypeSymbol, string> rootNamespaces)
    {
        TracksPerRoot = tracksPerRoot;
        RootNamespaces = rootNamespaces;
    }
}
#pragma warning restore S1192 // Enable analyzer release tracking
