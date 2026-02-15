#nullable enable

namespace Afrowave.AJIS.IO;

/// <summary>
/// Specifies the format used for storing AJIS data (AJIS text or ATP with attachments).
/// </summary>
/// <remarks>
/// <para>
/// AJIS is designed for text-based data, while ATP (Attachment Transfer Protocol) is optimized
/// for data with binary attachments, avoiding base64 encoding overhead.
/// </para>
/// <para>
/// Use <see cref="Auto"/> for automatic detection based on file extension and content.
/// </para>
/// </remarks>
public enum AjisFormat
{
    /// <summary>
    /// Automatic detection based on file extension and content.
    /// - .atp files always use ATP format
    /// - .ajis/.json files use AJIS format unless they contain binary attachments
    /// - Files with binary attachments automatically use ATP format
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Force AJIS text format (JSON-like).
    /// Use this for pure text data without binary attachments.
    /// </summary>
    Ajis = 1,

    /// <summary>
    /// Force ATP format with native binary attachments.
    /// Use this when you have binary data and want efficient storage without base64 encoding.
    /// </summary>
    Atp = 2
}
