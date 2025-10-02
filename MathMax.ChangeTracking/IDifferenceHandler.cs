using System.Text.RegularExpressions;

namespace MathMax.ChangeTracking;

public interface IDifferenceHandler<in TModel, in TEntity>
{
    Regex PathPattern { get; }
    /// <summary>
    /// Handle a difference. The provided <paramref name="match"/> is the successful Regex.Match result
    /// from <see cref="PathPattern"/> against <see cref="Difference.Path"/>, avoiding re-evaluating the pattern.
    /// </summary>
    /// <param name="diff">The difference instance.</param>
    /// <param name="match">The Regex match result (always Success = true).</param>
    /// <param name="original">The original object graph.</param>
    /// <param name="altered">The altered object graph.</param>
    /// <param name="entity">The entity to be modified.</param>
    void Handle(Difference diff, Match match, TModel original, TModel altered, TEntity entity);
}
