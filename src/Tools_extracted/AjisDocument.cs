using System;
using System.Text;
using System.Text.Json;

namespace Afrowave.AJIS;

/// <summary>
/// Represents an AJIS/JSON document.
/// This is the main entry point for parsing and working with AJIS data.
/// </summary>
/// <example>
/// <code>
/// // Parse JSON/AJIS
/// var doc = AjisDocument.Parse("{\"name\":\"John\",\"age\":30}");
///
/// // Access values easily
/// string name = doc.Root["name"].AsString();
/// int age = doc.Root["age"].AsInt32();
/// </code>
/// </example>
public sealed partial class AjisDocument
{
    /// <summary>
    /// Gets the root value of this document.
    /// </summary>
    public AjisValue Root { get; }

    /// <summary>
    /// Gets the lexer options used to parse this document.
    /// </summary>
    public AjisLexerOptions LexerOptions { get; }

    /// <summary>
    /// Gets the parser options used to parse this document.
    /// </summary>
    public AjisParserOptions ParserOptions { get; }

    /// <summary>
    /// Initializes a new document with the given root value.
    /// </summary>
    public AjisDocument(AjisValue root, AjisLexerOptions? lexerOptions = null, AjisParserOptions? parserOptions = null)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        LexerOptions = lexerOptions ?? AjisLexerOptions.Ajis;
        ParserOptions = parserOptions ?? AjisParserOptions.Ajis;
    }

    /// <summary>
    /// Parses an AJIS string into a document (Ajis-first).
    /// JSON compatibility is available via AjisLexerOptions.Json and AjisParserOptions.Json.
    /// OPTIMIZED: Uses Utf8JsonReader for maximum performance.
    /// </summary>
    /// <param name="text">The JSON/AJIS text to parse.</param>
    /// <param name="lexerOptions">Lexer options (null for JSON-compatible mode).</param>
    /// <param name="parserOptions">Parser options (null for AJIS mode).</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="AjisParseException">Thrown when the input is invalid.</exception>
    /// <example>
    /// <code>
    /// var doc = AjisDocument.Parse("{\"key\":\"value\"}");
    ///
    /// // Parse with camelCase to PascalCase conversion
    /// var options = new AjisParserOptions
    /// {
    ///     PropertyNamingPolicy = AjisNamingPolicy.CamelCase
    /// };
    /// var doc2 = AjisDocument.Parse("{\"myKey\":\"value\"}", null, options);
    /// // doc2.Root["MyKey"] will work (converted from myKey)
    /// </code>
    /// </example>
    public static AjisDocument Parse(string text, AjisLexerOptions? lexerOptions = null, AjisParserOptions? parserOptions = null)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        return Parse(text.AsMemory(), lexerOptions, parserOptions);
    }

    /// <summary>
    /// Parses from a character memory buffer (avoids extra substring allocations).
    /// </summary>
    public static AjisDocument Parse(ReadOnlyMemory<char> text, AjisLexerOptions? lexerOptions = null, AjisParserOptions? parserOptions = null)
    {
        // Prefer Utf8-based parser when lazy string materialization is enabled
        // as it can avoid many string allocations by keeping UTF-8 backing buffers.
        var lexerOpts = lexerOptions ?? AjisLexerOptions.Ajis;
        var options = parserOptions ?? AjisParserOptions.Ajis;

        // If caller requested strict JSON semantics, use System.Text.Json fast-path
        if (parserOptions != null && parserOptions.AllowNumericSeparators == false && parserOptions.AllowExtendedNumberFormats == false && parserOptions.AllowComments == false && parserOptions.AllowTrailingCommas == false)
        {
            // Parse using JsonDocument then convert to AjisValue
            var str = text.ToString();
            using var doc = JsonDocument.Parse(str);
            var jsonRoot = ConvertJsonElement(doc.RootElement);
            return new AjisDocument(jsonRoot, lexerOpts, options);
        }

        // Use the character-based parser for string input to ensure lexer-level errors
        // and AJIS lexical rules are applied deterministically.
        var parser = new AjisParser(text, lexerOpts, options);
        var root = parser.Parse();

        return new AjisDocument(root, lexerOpts, options);
    }

    /// <summary>
    /// Parses directly from UTF-8 encoded bytes. This favors the Utf8-based parser for performance.
    /// </summary>
    public static AjisDocument Parse(ReadOnlySpan<byte> utf8Json, AjisParserOptions? parserOptions = null)
    {
        var options = parserOptions ?? AjisParserOptions.Ajis;

        // If strict JSON semantics requested, use JsonDocument fast-path
        if (parserOptions != null && options.AllowNumericSeparators == false && options.AllowExtendedNumberFormats == false && options.AllowComments == false && options.AllowTrailingCommas == false)
        {
            // JsonDocument does not provide a Parse(ReadOnlySpan<byte>) overload in all target frameworks,
            // so create a temporary array for the fast-path.
            var bytes = utf8Json.ToArray();
            using var doc = JsonDocument.Parse(bytes);
            var rootFromJson = ConvertJsonElement(doc.RootElement);
            return new AjisDocument(rootFromJson, AjisLexerOptions.Ajis, options);
        }

        // Prefer Utf8 parser for byte input
        var ajisRoot = AjisUtf8Parser.Parse(utf8Json, options);
        return new AjisDocument(ajisRoot, AjisLexerOptions.Ajis, options);
    }

    private static AjisValue ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var dict = new Dictionary<string, AjisValue>(StringComparer.Ordinal);
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ConvertJsonElement(prop.Value);
                }
                return AjisValue.Object(dict);
            }
            case JsonValueKind.Array:
            {
                var list = new List<AjisValue>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ConvertJsonElement(item));
                }
                return AjisValue.Array(list);
            }
            case JsonValueKind.String:
            {
                var s = element.GetString() ?? string.Empty;
                return AjisValue.String(s);
            }
            case JsonValueKind.Number:
            {
                if (element.TryGetInt64(out var l)) return AjisValue.Number(l);
                if (element.TryGetDouble(out var d)) return AjisValue.Number(d);
                // Fallback to string representation then parse
                var numStr = element.GetRawText();
                if (long.TryParse(numStr, out l)) return AjisValue.Number(l);
                if (double.TryParse(numStr, out d)) return AjisValue.Number(d);
                return AjisValue.String(numStr);
            }
            case JsonValueKind.True:
                return AjisValue.Boolean(true);
            case JsonValueKind.False:
                return AjisValue.Boolean(false);
            case JsonValueKind.Null:
            default:
                return AjisValue.Null();
        }
    }

    /// <summary>
    /// Parses directly from a UTF-8 encoded byte array.
    /// </summary>
    public static AjisDocument Parse(byte[] utf8Json, AjisParserOptions? parserOptions = null)
    {
        if (utf8Json == null) throw new ArgumentNullException(nameof(utf8Json));
        var root = AjisUtf8Parser.Parse(utf8Json.AsSpan(), parserOptions);
        return new AjisDocument(root, AjisLexerOptions.Ajis, parserOptions);
    }

    /// <summary>
    /// Parses an AJIS/JSON string with all AJIS extensions enabled.
    /// </summary>
    /// <param name="text">The AJIS text to parse.</param>
    /// <returns>The parsed document.</returns>
    public static AjisDocument ParseAjis(string text)
    {
        return Parse(text, AjisLexerOptions.Ajis);
    }

    /// <summary>
    /// Indexer for convenient root access.
    /// </summary>
    public AjisValue this[string key] => Root[key];

    /// <summary>
    /// Indexer for convenient root access.
    /// </summary>
    public AjisValue this[int index] => Root[index];

    public override string ToString() => Root.ToString();
}
