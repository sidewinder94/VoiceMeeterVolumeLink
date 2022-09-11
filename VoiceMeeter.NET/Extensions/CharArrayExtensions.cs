namespace VoiceMeeter.NET.Extensions;

public static class CharArrayExtensions
{
    public static string GetStringFromNullTerminatedCharArray(this char[] nullTerminatedArray)
    {
        Span<char> charSpan = nullTerminatedArray.AsSpan();
        int terminator = charSpan.IndexOf('\0');

        return new string(charSpan[..terminator]);
    }
}