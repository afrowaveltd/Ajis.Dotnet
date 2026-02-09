// Stub types for legacy parser integration
// These are minimal implementations to make old parsers compile

namespace Afrowave.AJIS.Benchmarks.Legacy;

// Stub for AjisLocation
internal record struct AjisLocation(long Position, int Line, int Column);

// Stub for AjisLexerOptions
internal class AjisLexerOptions
{
    public static AjisLexerOptions Default => new();
    public bool AllowComments { get; set; } = true;
    public bool AllowTrailingCommas { get; set; } = true;
    public int MaxDepth { get; set; } = 64;
}

// Stub for AjisNamingPolicy
internal abstract class AjisNamingPolicy
{
    public abstract string ConvertName(string name);
    public static AjisNamingPolicy CamelCase => new CamelCasePolicy();
    
    private class CamelCasePolicy : AjisNamingPolicy
    {
        public override string ConvertName(string name) => 
            string.IsNullOrEmpty(name) ? name : 
            char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}

// Stub for I18n (if needed)
internal static class I18n
{
    public static string GetString(string key) => key;
}
