using Microsoft.CodeAnalysis;

namespace MathMax.Generators.ChangeTracking;

public class TrackInfo
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
