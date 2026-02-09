using System;

namespace Afrowave.AJIS;

/// <summary>
/// Exception thrown when a lexical error occurs.
/// </summary>
public class AjisLexerException : AjisException
{
    /// <summary>
    /// The location where the error occurred.
    /// </summary>
    public AjisLocation Location { get; }

    public AjisLexerException(string message, AjisLocation location)
        : base($"{message} at {location}")
    {
        Location = location;
    }

    public AjisLexerException(string message, AjisLocation location, Exception? innerException)
        : base($"{message} at {location}", innerException)
    {
        Location = location;
    }
}
