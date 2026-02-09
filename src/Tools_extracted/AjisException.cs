using System;
using Afrowave.AJIS.I18n;

namespace Afrowave.AJIS.Legacy;

/// <summary>
/// Base exception for all AJIS errors.
/// </summary>
public class AjisException : Exception
{
    public AjisException(string message) : base(message) { }

    public AjisException(string message, Exception? innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Creates a localized exception.
    /// </summary>
    public static AjisException Localized(string key, params object[] args)
    {
        var message = AjisLocalizer.Instance.GetFormat(key, args);
        return new AjisException(message);
    }
}
