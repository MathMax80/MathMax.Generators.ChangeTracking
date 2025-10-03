namespace MathMax.ChangeTracking;

public class DifferenceDispatchResult
{
    public Difference[] Handled { get; set; } = [];
    public Difference[] Unhandled { get; set; } = [];
}