using System.Collections.Generic;

namespace MathMax.ChangeTracking;

public interface IDifferenceDispatcher<in TModel, in TEntity>
{
    DifferenceDispatchResult Dispatch(IEnumerable<Difference> diffs, TModel original, TModel altered, TEntity entity);
}
