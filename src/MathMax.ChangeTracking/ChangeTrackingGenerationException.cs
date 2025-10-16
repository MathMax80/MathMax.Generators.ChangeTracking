namespace MathMax.ChangeTracking;

[System.Serializable]
public class ChangeTrackingGenerationException : System.Exception
{
    public ChangeTrackingGenerationException() { }
    public ChangeTrackingGenerationException(string message) : base(message) { }
    public ChangeTrackingGenerationException(string message, System.Exception inner) : base(message, inner) { }
}