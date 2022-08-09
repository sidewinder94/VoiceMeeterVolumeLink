namespace VoiceMeeter.NET.Exceptions;

public class VoiceMeeterException : Exception
{
    public VoiceMeeterException() : base()
    {}
    
    public VoiceMeeterException(string message) : base(message)
    {
        
    }
}