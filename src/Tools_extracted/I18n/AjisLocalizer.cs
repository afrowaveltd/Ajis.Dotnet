using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace Afrowave.AJIS.I18n;

/// <summary>
/// Provides localization support for AJIS error messages.
/// Supports .loc and .ajis dictionary files.
/// </summary>
public sealed class AjisLocalizer
{
    private static AjisLocalizer? _instance;
    private static readonly object _lock = new object();

    private readonly Dictionary<string, Dictionary<string, string>> _translations;
    private string _currentLocale;

    private AjisLocalizer()
    {
        _translations = new Dictionary<string, Dictionary<string, string>>();
        _currentLocale = "en";
    }

    /// <summary>
    /// Gets the singleton instance of the localizer.
    /// </summary>
    public static AjisLocalizer Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new AjisLocalizer();
                        _instance.LoadDefaultLocales();
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Gets or sets the current locale (e.g., "en", "cs").
    /// </summary>
    public string CurrentLocale
    {
        get => _currentLocale;
        set
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));
            _currentLocale = value;
        }
    }

    /// <summary>
    /// Loads locale file from path. Supports .loc and .ajis formats.
    /// </summary>
    /// <param name="localePath">Path to .loc or .ajis file</param>
    /// <param name="locale">Locale identifier (e.g., "en", "cs")</param>
    public void LoadLocale(string localePath, string locale)
    {
        if (!File.Exists(localePath))
            throw new FileNotFoundException($"Locale file not found: {localePath}");

        var extension = Path.GetExtension(localePath).ToLowerInvariant();
        var translations = extension switch
        {
            ".ajis" => LoadAjisFormat(localePath),
            ".loc" => LoadLocFormat(localePath),
            _ => throw new NotSupportedException($"Unsupported locale file format: {extension}")
        };

        lock (_lock)
        {
            _translations[locale] = translations;
        }
    }

    /// <summary>
    /// Loads all .loc and .ajis files from a directory.
    /// </summary>
    /// <param name="localesDirectory">Directory containing locale files</param>
    public void LoadLocalesFromDirectory(string localesDirectory)
    {
        if (!Directory.Exists(localesDirectory))
            return;

        foreach (var file in Directory.GetFiles(localesDirectory, "*.*"))
        {
            var extension = Path.GetExtension(file).ToLowerInvariant();
            if (extension == ".loc" || extension == ".ajis")
            {
                var locale = Path.GetFileNameWithoutExtension(file);
                LoadLocale(file, locale);
            }
        }
    }

    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    /// <param name="key">Translation key</param>
    /// <param name="fallback">Fallback text if key not found</param>
    /// <returns>Localized string or fallback</returns>
    public string Get(string key, string? fallback = null)
    {
        // Try current locale
        if (_translations.TryGetValue(_currentLocale, out var dict))
        {
            if (dict.TryGetValue(key, out var value))
                return value;
        }

        // Try English as fallback
        if (_currentLocale != "en" && _translations.TryGetValue("en", out var enDict))
        {
            if (enDict.TryGetValue(key, out var value))
                return value;
        }

        // Return fallback or key itself
        return fallback ?? key;
    }

    /// <summary>
    /// Gets a localized string with format arguments.
    /// </summary>
    public string GetFormat(string key, params object[] args)
    {
        var template = Get(key);
        return string.Format(template, args);
    }

    private Dictionary<string, string> LoadAjisFormat(string path)
    {
        var content = File.ReadAllText(path);
        var doc = AjisDocument.Parse(content);

        var result = new Dictionary<string, string>();
        foreach (var kvp in doc.Root.AsObject())
        {
            result[kvp.Key] = kvp.Value.AsString();
        }

        return result;
    }

    private Dictionary<string, string> LoadLocFormat(string path)
    {
        var result = new Dictionary<string, string>();
        var lines = File.ReadAllLines(path);

        foreach (var line in lines)
        {
            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                continue;

            var colonIndex = line.IndexOf(':');
            if (colonIndex < 0)
                continue;

            var key = line.Substring(0, colonIndex).Trim();
            var value = line.Substring(colonIndex + 1).Trim();

            // Remove quotes if present
            if (value.StartsWith('"') && value.EndsWith('"'))
                value = value.Substring(1, value.Length - 2);

            // Decode escape sequences
            value = DecodeEscapes(value);

            result[key] = value;
        }

        return result;
    }

    private string DecodeEscapes(string input)
    {
        return input
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t")
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\");
    }

    private void LoadDefaultLocales()
    {
        // Load embedded default translations
        _translations["en"] = new Dictionary<string, string>
        {
            ["error.unexpected_token"] = "Unexpected token: {0}",
            ["error.unexpected_eof"] = "Unexpected end of file",
            ["error.invalid_number"] = "Invalid number format: {0}",
            ["error.invalid_string"] = "Invalid string format",
            ["error.unterminated_string"] = "Unterminated string",
            ["error.duplicate_key"] = "Duplicate key '{0}' in object",
            ["error.max_depth"] = "Maximum parsing depth ({0}) exceeded",
            ["error.trailing_comma"] = "Trailing comma not allowed",
            ["error.expected_token"] = "Expected {0}, got {1}",
            ["error.invalid_escape"] = "Invalid escape sequence: \\{0}",
            ["error.leading_zeros"] = "Leading zeros not allowed in numbers",
            ["error.invalid_keyword"] = "Invalid keyword: {0}",
            ["error.expected_string_key"] = "Expected string key in object, got {0}",
            ["error.expected_colon"] = "Expected ':' after object key",
            ["error.expected_comma_or_close"] = "Expected ',' or '{0}' in {1}",
            ["error.numeric_separators_not_allowed"] = "Numeric separators not allowed",
            ["error.extended_formats_not_allowed"] = "Extended number formats not allowed",
        };
    }

    /// <summary>
    /// Detects and sets locale from system culture.
    /// </summary>
    public void UseSystemLocale()
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (_translations.ContainsKey(culture))
        {
            CurrentLocale = culture;
        }
    }
}
