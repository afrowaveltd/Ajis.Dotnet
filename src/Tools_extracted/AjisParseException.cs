using System;

namespace Afrowave.AJIS.Legacy;

/// <summary>
/// Exception thrown when a parse error occurs.
/// </summary>
public class AjisParseException : AjisException
{
    /// <summary>
    /// The location where the error occurred.
    /// </summary>
    public AjisLocation Location { get; }

    public AjisParseException(string message, AjisLocation location)
        : base($"{message} at {location}")
    {
        Location = location;
    }

    public AjisParseException(string message, AjisLocation location, Exception? innerException)
        : base($"{message} at {location}", innerException)
    {
        Location = location;
    }
}
