using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MathMax.ChangeTracking;

public class DifferenceDispatcher<TModel, TEntity> : IDifferenceDispatcher<TModel, TEntity>
{
    private readonly IDifferenceHandler<TModel, TEntity>[] _handlers;

    public DifferenceDispatcher(IEnumerable<IDifferenceHandler<TModel, TEntity>> handlers)
    {
        _handlers = [.. handlers];
    }

    public DifferenceDispatchResult Dispatch(IEnumerable<Difference> diffs, TModel original, TModel altered, TEntity entity)
    {
        List<Difference> handled = [];
        List<Difference> unhandled = [];

        foreach (var diff in diffs)
        {
            // Evaluate each handler's regex once and pass the Match to avoid duplicate regex executions inside handlers.
            var matchingHandlers = _handlers
                .Select(h => (Handler: h, Match: h.PathPattern.Match(diff.Path)))
                .Where(x => x.Match.Success)
                .ToList();

            if (matchingHandlers.Count > 0)
            {
                foreach (var (handler, match) in matchingHandlers)
                {
                    handler.Handle(diff, match, original, altered, entity);
                }
                handled.Add(diff);
            }
            else
            {
                unhandled.Add(diff);
            }
        }

        return new DifferenceDispatchResult
        {
            Handled = [.. handled],
            Unhandled = [.. unhandled]
        };
    }
}
