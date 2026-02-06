#nullable enable

using Afrowave.AJIS.Core.Abstraction;
using Afrowave.AJIS.Core.Events;
using System.Globalization;

namespace Afrowave.AJIS.Core.Configuration;

/// <summary>
/// Shared settings for AJIS operations (parsing, serialization, streaming).
/// </summary>
public sealed class AjisSettings
{
   /// <summary>
   /// Culture used by AJIS for formatting and localized diagnostics.
   /// If null, current process cultures are used.
   /// </summary>
   public CultureInfo? Culture { get; set; } = null;

   /// <summary>Localization provider (optional). If null, caller should use defaults.</summary>
   public IAjisTextProvider? TextProvider { get; set; } = null;

   /// <summary>Event sink for progress/diagnostics (optional).</summary>
   public IAjisEventSink EventSink { get; set; } = NullAjisEventSink.Instance;

   /// <summary>Maximum container nesting depth.</summary>
   public int MaxDepth { get; set; } = 256;

   /// <summary>Property naming strategy for mapping AJIS fields to C# members (where applicable).</summary>
   public AjisPropertyNaming Naming { get; set; } = AjisPropertyNaming.PascalCase;

   /// <summary>
   /// Threshold for using chunked memory-mapped reading (e.g. "2G", "512M", "1k").
   /// If no suffix is provided, value is treated as megabytes.
   /// </summary>
   public string StreamChunkThreshold { get; set; } = "2G";

   /// <summary>
   /// Processing profile for parser selection.
   /// </summary>
   public AjisProcessingProfile ParserProfile { get; set; } = AjisProcessingProfile.Universal;

   /// <summary>
   /// Processing profile for serializer selection.
   /// </summary>
   public AjisProcessingProfile SerializerProfile { get; set; } = AjisProcessingProfile.Universal;

   /// <summary>
   /// Text parsing mode (Json, Ajis, Lex).
   /// </summary>
   public AjisTextMode TextMode { get; set; } = AjisTextMode.Ajis;

   /// <summary>
   /// Controls whether directives are allowed.
   /// </summary>
   public bool AllowDirectives { get; set; } = true;

   /// <summary>
   /// Controls whether trailing commas are allowed in arrays and objects.
   /// </summary>
   public bool AllowTrailingCommas { get; set; } = false;

   /// <summary>
   /// String parsing options.
   /// </summary>
   public AjisStringOptions Strings { get; set; } = new();
}

/// <summary>
/// Naming strategy for property mapping.
/// </summary>
public enum AjisPropertyNaming
{
   PascalCase = 0,
   CamelCase = 1,
   AsIs = 2,
}