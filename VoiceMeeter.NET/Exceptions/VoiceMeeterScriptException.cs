namespace VoiceMeeter.NET.Exceptions;

public sealed class VoiceMeeterScriptException : Exception
{
    public string Script { get; }

    public VoiceMeeterScriptException(string message, string script) : base(message)
    {
        this.Script = script;
        this.Data["Script"] = script;
    }
}