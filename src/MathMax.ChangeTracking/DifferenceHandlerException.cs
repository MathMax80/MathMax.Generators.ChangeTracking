namespace MathMax.ChangeTracking;

[System.Serializable]
public class DifferenceHandlerException : System.Exception
{
    public DifferenceHandlerException() { }
    public DifferenceHandlerException(string message) : base(message) { }
    public DifferenceHandlerException(string message, System.Exception inner) : base(message, inner) { }
}
