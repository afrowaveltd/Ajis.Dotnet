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