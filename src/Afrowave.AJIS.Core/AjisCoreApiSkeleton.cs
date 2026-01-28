#nullable enable

using System.Globalization;

namespace Afrowave.AJIS.Core;

/// <summary>
/// Central configuration object for AJIS parsing, serialization, diagnostics and optional extensions.
/// </summary>
/// <remarks>
/// <para>
/// AJIS is streaming-first. Settings are designed so that the streaming parser and serializer can
/// operate without materializing a full DOM.
/// </para>
/// <para>
/// Prefer to keep <see cref="AjisSettings"/> immutable in user code.
/// </para>
/// </remarks>
public sealed class AjisSettings
{
   /// <summary>
   /// Creates settings with AJIS defaults.
   /// </summary>
   public AjisSettings()
   {
   }

   /// <summary>
   /// Gets or sets number parsing/formatting rules.
   /// </summary>
   public AjisNumberOptions Numbers { get; init; } = new();

   /// <summary>
   /// Gets or sets string parsing/formatting rules.
   /// </summary>
   public AjisStringOptions Strings { get; init; } = new();

   /// <summary>
   /// Gets or sets comment parsing rules.
   /// </summary>
   public AjisCommentOptions Comments { get; init; } = new();

   /// <summary>
   /// Gets or sets serialization preferences.
   /// </summary>
   public AjisSerializationOptions Serialization { get; init; } = new();

   /// <summary>
   /// Controls whether duplicate keys in objects are allowed.
   /// </summary>
   /// <remarks>
   /// When false, duplicate keys must produce a diagnostic with an error code, and may fail parsing
   /// depending on the caller policy.
   /// </remarks>
   public bool AllowDuplicateObjectKeys { get; init; } = false;

   /// <summary>
   /// Controls whether trailing commas are allowed in arrays and objects.
   /// </summary>
   public bool AllowTrailingCommas { get; init; } = false;

   /// <summary>
   /// Maximum allowed nesting depth.
   /// </summary>
   public int MaxDepth { get; init; } = 256;

   /// <summary>
   /// Optional localization provider. If not provided, tools may fall back to invariant English.
   /// </summary>
   public IAjisTextProvider? TextProvider { get; init; }

   /// <summary>
   /// Optional logger. If null, AJIS emits no logs.
   /// </summary>
   public IAjisLogger? Logger { get; init; }

   /// <summary>
   /// Optional event sink used to stream progress, phases and diagnostics.
   /// </summary>
   public IAjisEventSink? EventSink { get; init; }

   /// <summary>
   /// Culture used for formatting (not parsing) when applicable.
   /// </summary>
   /// <remarks>
   /// Parsing is culture-invariant.
   /// </remarks>
   public CultureInfo FormattingCulture { get; init; } = CultureInfo.InvariantCulture;
}

/// <summary>
/// Number handling options for AJIS.
/// </summary>
public sealed class AjisNumberOptions
{
   /// <summary>
   /// Enables support for non-decimal bases (0b, 0o, 0x).
   /// </summary>
   public bool EnableBasePrefixes { get; init; } = true;

   /// <summary>
   /// Enables digit separators using underscore (<c>_</c>).
   /// </summary>
   public bool EnableDigitSeparators { get; init; } = true;

   /// <summary>
   /// When enabled, AJIS enforces underscore grouping rules per spec (binary=4, oct/dec=3, hex=2 or 4).
   /// </summary>
   public bool EnforceSeparatorGroupingRules { get; init; } = true;
}

/// <summary>
/// String handling options for AJIS.
/// </summary>
public sealed class AjisStringOptions
{
   /// <summary>
   /// Enables multi-line strings (line breaks allowed inside quotes).
   /// </summary>
   public bool AllowMultiline { get; init; } = true;

   /// <summary>
   /// Enables processing of escape sequences such as <c>\n</c>, <c>\t</c>, and <c>\uXXXX</c>.
   /// </summary>
   public bool EnableEscapes { get; init; } = true;
}

/// <summary>
/// Comment handling options for AJIS.
/// </summary>
public sealed class AjisCommentOptions
{
   /// <summary>
   /// Enables line comments (<c>// ...</c>).
   /// </summary>
   public bool AllowLineComments { get; init; } = true;

   /// <summary>
   /// Enables block comments (<c>/* ... */</c>).
   /// </summary>
   public bool AllowBlockComments { get; init; } = true;

   /// <summary>
   /// When true, nested block comments are rejected.
   /// </summary>
   public bool RejectNestedBlockComments { get; init; } = true;
}

/// <summary>
/// Serialization preferences.
/// </summary>
public sealed class AjisSerializationOptions
{
   /// <summary>
   /// If true, serializer emits compact output with minimal whitespace.
   /// </summary>
   public bool Compact { get; init; } = false;

   /// <summary>
   /// If true, serializer emits deterministic canonical output.
   /// </summary>
   public bool Canonicalize { get; init; } = false;

   /// <summary>
   /// If true, output is limited to strict JSON features (no comments, no non-decimal bases, etc.).
   /// </summary>
   public bool JsonCompatible { get; init; } = false;

   /// <summary>
   /// Indentation size used when <see cref="Compact"/> is false.
   /// </summary>
   public int IndentSize { get; init; } = 2;
}

/// <summary>
/// Represents a position in AJIS text.
/// </summary>
public readonly record struct AjisTextPosition(long ByteOffset, int Line, int Column)
{
   /// <summary>
   /// A zero position (start of file).
   /// </summary>
   public static AjisTextPosition Zero => new(0, 1, 1);

   /// <inheritdoc />
   public override string ToString() => $"{Line}:{Column} (+{ByteOffset}B)";
}

/// <summary>
/// Diagnostic severity.
/// </summary>
public enum AjisDiagnosticSeverity
{
   /// <summary>
   /// Informational message.
   /// </summary>
   Info = 0,

   /// <summary>
   /// Warning that does not necessarily prevent further processing.
   /// </summary>
   Warning = 1,

   /// <summary>
   /// Error that indicates invalid input or unrecoverable issue.
   /// </summary>
   Error = 2,
}

/// <summary>
/// Stable diagnostic codes for AJIS.
/// </summary>
public enum AjisErrorCode
{
   // General
   Unknown = 0,

   // Structure
   UnexpectedToken = 1000,
   UnexpectedEndOfInput = 1001,
   MaxDepthExceeded = 1002,

   // Strings
   UnterminatedString = 2000,
   InvalidEscapeSequence = 2001,

   // Numbers
   InvalidNumber = 3000,
   InvalidBasePrefix = 3001,
   InvalidDigitSeparator = 3002,

   // Objects/Arrays
   DuplicateKey = 4000,
   TrailingCommaNotAllowed = 4001,
}

/// <summary>
/// A structured diagnostic produced by parsers/serializers/tools.
/// </summary>
public sealed record AjisDiagnostic(
    AjisErrorCode Code,
    AjisDiagnosticSeverity Severity,
    AjisTextPosition Position,
    string MessageKey,
    IReadOnlyDictionary<string, object?>? Data = null)
{
   /// <summary>
   /// Creates a human-readable message using <paramref name="provider"/>.
   /// </summary>
   public string FormatMessage(IAjisTextProvider? provider, CultureInfo? culture = null)
   {
      provider ??= DefaultAjisTextProvider.Instance;
      culture ??= CultureInfo.CurrentUICulture;
      return provider.GetText(MessageKey, culture, Data);
   }
}

/// <summary>
/// Base exception for AJIS.
/// </summary>
public class AjisException : Exception
{
   /// <summary>
   /// Initializes a new instance.
   /// </summary>
   public AjisException(AjisErrorCode code, AjisTextPosition position, string messageKey, Exception? inner = null)
       : base(messageKey, inner)
   {
      Code = code;
      Position = position;
      MessageKey = messageKey;
   }

   /// <summary>
   /// Diagnostic code associated with this exception.
   /// </summary>
   public AjisErrorCode Code { get; }

   /// <summary>
   /// Position of the error.
   /// </summary>
   public AjisTextPosition Position { get; }

   /// <summary>
   /// Localization key.
   /// </summary>
   public string MessageKey { get; }
}

/// <summary>
/// Exception thrown for invalid AJIS text.
/// </summary>
public sealed class AjisFormatException : AjisException
{
   /// <summary>
   /// Initializes a new instance.
   /// </summary>
   public AjisFormatException(AjisErrorCode code, AjisTextPosition position, string messageKey, Exception? inner = null)
       : base(code, position, messageKey, inner)
   {
   }
}

/// <summary>
/// Abstraction for providing localized texts.
/// </summary>
public interface IAjisTextProvider
{
   /// <summary>
   /// Gets a localized string for <paramref name="key"/>.
   /// </summary>
   /// <param name="key">Message key (stable).</param>
   /// <param name="culture">UI culture.</param>
   /// <param name="data">Optional data used for interpolation.</param>
   /// <returns>Localized text.</returns>
   string GetText(string key, CultureInfo culture, IReadOnlyDictionary<string, object?>? data = null);
}

internal sealed class DefaultAjisTextProvider : IAjisTextProvider
{
   public static DefaultAjisTextProvider Instance { get; } = new();

   private DefaultAjisTextProvider() { }

   public string GetText(string key, CultureInfo culture, IReadOnlyDictionary<string, object?>? data = null)
   {
      // Intentionally minimal fallback. Tools can provide richer dictionaries.
      // Do not throw here.
      return key;
   }
}

/// <summary>
/// Logger abstraction for AJIS. Implementations may forward to Serilog, Microsoft.Extensions.Logging, etc.
/// </summary>
public interface IAjisLogger
{
   /// <summary>
   /// Logs a message.
   /// </summary>
   void Log(AjisLogLevel level, string messageKey, Exception? exception = null, IReadOnlyDictionary<string, object?>? data = null);
}

/// <summary>
/// Log level.
/// </summary>
public enum AjisLogLevel
{
   Debug = 0,
   Info = 1,
   Warning = 2,
   Error = 3,
   Critical = 4,
}

/// <summary>
/// Base type for streamed events emitted during parsing/serialization/tools.
/// </summary>
public abstract record AjisEvent(DateTimeOffset Timestamp);

/// <summary>
/// Represents progress information for long-running operations.
/// </summary>
/// <param name="Timestamp">Event time.</param>
/// <param name="Phase">A short phase identifier (e.g. "parse", "serialize", "copy").</param>
/// <param name="ProcessedBytes">Processed bytes so far (if available).</param>
/// <param name="TotalBytes">Total bytes (if known).</param>
/// <param name="Percent">Computed percentage (0..100) if possible.</param>
public sealed record AjisProgressEvent(
    DateTimeOffset Timestamp,
    string Phase,
    long ProcessedBytes,
    long? TotalBytes,
    int? Percent) : AjisEvent(Timestamp);

/// <summary>
/// Streams diagnostics as events.
/// </summary>
public sealed record AjisDiagnosticEvent(DateTimeOffset Timestamp, AjisDiagnostic Diagnostic) : AjisEvent(Timestamp);

/// <summary>
/// Signals a phase change.
/// </summary>
public sealed record AjisPhaseEvent(DateTimeOffset Timestamp, string Phase, string? Detail = null) : AjisEvent(Timestamp);

/// <summary>
/// Receives AJIS events.
/// </summary>
/// <remarks>
/// Implementations should be fast. AJIS may call the sink frequently.
/// Consider buffering/throttling in the sink.
/// </remarks>
public interface IAjisEventSink
{
   /// <summary>
   /// Publishes an event.
   /// </summary>
   /// <remarks>
   /// This method is designed for fire-and-forget usage by AJIS.
   /// Implementations should not throw.
   /// </remarks>
   ValueTask PublishAsync(AjisEvent evt, CancellationToken ct = default);
}
