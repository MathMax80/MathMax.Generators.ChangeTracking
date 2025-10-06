using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MathMax.Generators.ChangeTracking.Tests;

/// <summary>
/// Helper for building (and caching) small in-memory Roslyn compilations from source files during tests.
/// </summary>
internal static class RoslynModelLoader
{
    private static readonly ConcurrentDictionary<string, Lazy<(CSharpCompilation Compilation, INamedTypeSymbol Root, IReadOnlyDictionary<string, IFieldSymbol> Fields)>> _cache = new();

    /// <summary>
    /// Loads (or returns a cached) Roslyn model for the given source file and root type name.
    /// The returned tuple includes the compilation, the root type symbol, and a dictionary of its field symbols by name.
    /// </summary>
    /// <param name="path">Relative or absolute path to the C# source file.</param>
    /// <param name="rootTypeName">The name of the root type (e.g., a class) to extract field symbols from.</param>
    public static (CSharpCompilation Compilation, INamedTypeSymbol Root, IReadOnlyDictionary<string, IFieldSymbol> Fields) LoadFromFile(string path, string rootTypeName)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path required", nameof(path));
        if (string.IsNullOrWhiteSpace(rootTypeName)) throw new ArgumentException("Root type name required", nameof(rootTypeName));

        var normalizedPath = NormalizePath(path);
        var cacheKey = normalizedPath + "::" + rootTypeName;

        var lazy = _cache.GetOrAdd(cacheKey, key => new Lazy<(CSharpCompilation, INamedTypeSymbol, IReadOnlyDictionary<string, IFieldSymbol>)>(
            () =>
            {
                var sepIndex = key.LastIndexOf("::", StringComparison.Ordinal);
                var filePath = key[..sepIndex];
                var typeName = key[(sepIndex + 2)..];

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Test source file not found: {filePath}");
                }

                var source = File.ReadAllText(filePath);
                var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview, DocumentationMode.Parse));

                var refs = new HashSet<MetadataReference>(MetadataReferenceComparer.Instance)
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Guid).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(TimeSpan).Assembly.Location),
                };

                var compilation = CSharpCompilation.Create(
                    assemblyName: $"TestModel_{Guid.NewGuid():N}",
                    syntaxTrees: new[] { tree },
                    references: refs,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

                var root = (INamedTypeSymbol?)compilation.GetSymbolsWithName(n => n == typeName).FirstOrDefault();
                if (root is null)
                {
                    throw new InvalidOperationException($"Root type '{typeName}' not found in source '{filePath}'.");
                }

                var fields = root.GetMembers().OfType<IFieldSymbol>().ToDictionary(f => f.Name, f => f);
                return (compilation, root, (IReadOnlyDictionary<string, IFieldSymbol>)fields);
            }, isThreadSafe: true));

        return lazy.Value;
    }

    private static string NormalizePath(string path)
    {
        if (Path.IsPathRooted(path)) return Path.GetFullPath(path);
        var baseDir = AppContext.BaseDirectory; // test bin folder
        var combined = Path.Combine(baseDir, path);
        return Path.GetFullPath(combined);
    }
}