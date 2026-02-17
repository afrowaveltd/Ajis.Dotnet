#nullable enable

namespace Afrowave.AJIS.IO;

/// <summary>
/// Options for JSON to AJIS/ATP conversion.
/// </summary>
public sealed class JsonConversionOptions
{
   /// <summary>
   /// Enable automatic binary detection and extraction.
   /// Default: true
   /// </summary>
   public bool AutoDetectBinary { get; set; } = true;

   /// <summary>
   /// Enable compression for the output file.
   /// Default: false
   /// </summary>
   public bool Compression { get; set; } = false;

   /// <summary>
   /// Output directory for extracted binary files (when saving binaries separately).
   /// Default: null (saves alongside the output file)
   /// </summary>
   public string? BinaryOutputDirectory { get; set; }

   /// <summary>
   /// Save extracted binaries as separate files.
   /// Default: false (embed in ATP)
   /// </summary>
   public bool SaveBinariesSeparately { get; set; } = false;
}