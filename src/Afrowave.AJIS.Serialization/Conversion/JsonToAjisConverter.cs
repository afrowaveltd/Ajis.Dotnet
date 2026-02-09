#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Afrowave.AJIS.Core;
using Afrowave.AJIS.Serialization.Mapping;

namespace Afrowave.AJIS.Serialization.Conversion;

/// <summary>
/// Converts JSON to AJIS format with automatic ATP (binary attachment) detection.
/// Creates .atp files (AJIS with ATP) for complete document storage.
/// </summary>
public sealed class JsonToAjisConverter
{
    private readonly AjisConverter<JsonElement> _ajisConverter;
    private readonly BinaryDetector _binaryDetector;
    private readonly List<(string Path, BinaryAttachment Attachment)> _detectedAttachments;

    public JsonToAjisConverter()
    {
        _ajisConverter = new AjisConverter<JsonElement>();
        _binaryDetector = new BinaryDetector();
        _detectedAttachments = new();
    }

    /// <summary>
    /// Converts JSON file to AJIS format with ATP support.
    /// Automatically detects and extracts binary data.
    /// </summary>
    public AjisConversionResult ConvertJsonToAjis(
        string jsonFilePath,
        bool detectBinary = true,
        ConversionOptions? options = null)
    {
        options ??= new();
        var result = new AjisConversionResult { SourceFile = jsonFilePath };

        try
        {
            // Read JSON
            var jsonContent = File.ReadAllText(jsonFilePath);
            result.OriginalSize = jsonContent.Length;

            // Parse JSON
            using (var jsonDoc = JsonDocument.Parse(jsonContent))
            {
                var root = jsonDoc.RootElement;

                // Process JSON
                JsonElement processedElement = root;

                if (detectBinary)
                {
                    Console.WriteLine("🔍 Detecting binary data...");
                    _detectedAttachments.Clear();
                    processedElement = ProcessJsonForBinary(root, "");
                    result.BinaryAttachmentsDetected = _detectedAttachments.Count;
                    result.DetectedAttachments = new(_detectedAttachments);
                }

                // Serialize to AJIS
                var ajisText = SerializeToAjisText(processedElement);
                result.AjisText = ajisText;
                result.AjisSize = Encoding.UTF8.GetByteCount(ajisText);
                result.SizeReduction = 100.0 * (1.0 - (double)result.AjisSize / result.OriginalSize);

                // If attachments detected, prepare ATP format
                if (_detectedAttachments.Count > 0)
                {
                    result.AtpMetadata = new()
                    {
                        CreatedDate = DateTime.UtcNow,
                        SourceFormat = "JSON",
                        ConversionMode = "Auto-ATP",
                        BinaryAttachmentCount = _detectedAttachments.Count,
                        BinaryAttachmentData = new(_detectedAttachments)
                    };
                }

                result.Success = true;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Saves AJIS data as .atp file with ATP metadata.
    /// </summary>
    public void SaveAsAtp(
        AjisConversionResult conversionResult,
        string outputFilePath)
    {
        // Create ATP wrapper
        var atpWrapper = new AtpFileWrapper
        {
            AjisContent = conversionResult.AjisText ?? "",
            Metadata = conversionResult.AtpMetadata ?? new(),
            Attachments = conversionResult.DetectedAttachments ?? new()
        };

        // Serialize to JSON with all metadata
        var atpJson = JsonSerializer.Serialize(atpWrapper, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        // Save .atp file
        File.WriteAllText(outputFilePath, atpJson, Encoding.UTF8);

        Console.WriteLine($"✅ Saved: {outputFilePath}");
        Console.WriteLine($"   Size: {new FileInfo(outputFilePath).Length} bytes");
    }

    /// <summary>
    /// Processes JSON and detects binary fields.
    /// </summary>
    private JsonElement ProcessJsonForBinary(JsonElement element, string path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var stringValue = element.GetString() ?? "";
                if (_binaryDetector.IsLikelyBinary(stringValue))
                {
                    var attachment = CreateAttachmentFromString(stringValue, path);
                    if (attachment != null)
                    {
                        _detectedAttachments.Add((path, attachment));
                        // Return placeholder
                        return JsonDocument.Parse($"{{\"type\":\"BinaryAttachment\",\"id\":\"{attachment.AttachmentId}\"}}").RootElement;
                    }
                }
                break;

            case JsonValueKind.Array:
                var jsonArray = element.EnumerateArray().ToList();
                // Check if array contains binary-like strings
                if (jsonArray.All(e => e.ValueKind == JsonValueKind.String && _binaryDetector.IsLikelyBinary(e.GetString() ?? "")))
                {
                    // Array of binary data
                    int index = 0;
                    foreach (var item in jsonArray)
                    {
                        var itemString = item.GetString() ?? "";
                        var attachment = CreateAttachmentFromString(itemString, $"{path}[{index}]");
                        if (attachment != null)
                        {
                            _detectedAttachments.Add(($"{path}[{index}]", attachment));
                        }
                        index++;
                    }
                }
                break;

            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var newPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                    ProcessJsonForBinary(property.Value, newPath);
                }
                break;
        }

        return element;
    }

    /// <summary>
    /// Creates BinaryAttachment from base64 or hex string.
    /// </summary>
    private BinaryAttachment? CreateAttachmentFromString(string stringValue, string path)
    {
        try
        {
            byte[]? data = null;

            // Try base64
            if (_binaryDetector.TryDecodeBase64(stringValue, out var base64Data))
            {
                data = base64Data;
            }
            // Try hex
            else if (_binaryDetector.TryDecodeHex(stringValue, out var hexData))
            {
                data = hexData;
            }

            if (data != null && data.Length > 0)
            {
                var imageType = DetectImageType(data);
                return new BinaryAttachment
                {
                    AttachmentId = Guid.NewGuid(),
                    FileName = $"{Path.GetFileNameWithoutExtension(path)}.{imageType.Extension}",
                    MimeType = imageType.MimeType,
                    Data = data
                };
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Detects image type from binary data.
    /// </summary>
    private ImageType DetectImageType(byte[] imageData)
    {
        if (imageData.Length < 4)
            return new("bin", "application/octet-stream");

        // PNG signature
        if (imageData[0] == 0x89 && imageData[1] == 0x50 &&
            imageData[2] == 0x4E && imageData[3] == 0x47)
            return new("png", "image/png");

        // JPG signature
        if (imageData[0] == 0xFF && imageData[1] == 0xD8)
            return new("jpg", "image/jpeg");

        // GIF signature
        if (imageData[0] == 0x47 && imageData[1] == 0x49 &&
            imageData[2] == 0x46)
            return new("gif", "image/gif");

        // WebP signature
        if (imageData.Length > 12 &&
            imageData[0] == 0x52 && imageData[1] == 0x49 &&
            imageData[2] == 0x46 && imageData[3] == 0x46)
            return new("webp", "image/webp");

        // BMP signature
        if (imageData[0] == 0x42 && imageData[1] == 0x4D)
            return new("bmp", "image/bmp");

        return new("bin", "application/octet-stream");
    }

    /// <summary>
    /// Serializes JSON element to AJIS text format.
    /// </summary>
    private string SerializeToAjisText(JsonElement element)
    {
        return JsonSerializer.Serialize(element, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}

/// <summary>
/// Detects binary data in strings.
/// </summary>
public sealed class BinaryDetector
{
    /// <summary>
    /// Checks if string is likely base64 or hex encoded binary.
    /// </summary>
    public bool IsLikelyBinary(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 20)
            return false;

        // Base64 check: starts with typical image/data signatures
        if (IsBase64(value))
        {
            return true;
        }

        // Hex check: all hex characters
        if (IsHex(value))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to decode base64 string.
    /// </summary>
    public bool TryDecodeBase64(string value, out byte[]? result)
    {
        result = null;
        try
        {
            result = Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to decode hex string.
    /// </summary>
    public bool TryDecodeHex(string value, out byte[]? result)
    {
        result = null;
        try
        {
            if (value.Length % 2 != 0)
                return false;

            result = new byte[value.Length / 2];
            for (int i = 0; i < value.Length; i += 2)
            {
                result[i / 2] = Convert.ToByte(value.Substring(i, 2), 16);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsBase64(string value)
    {
        // Check for base64 magic bytes (PNG, JPG, etc.)
        if (value.StartsWith("iVBORw0KGgo"))  // PNG
            return true;
        if (value.StartsWith("/9j/"))  // JPG
            return true;
        if (value.StartsWith("R0lGODlh"))  // GIF
            return true;

        // Try to decode to verify
        try
        {
            Convert.FromBase64String(value);
            // Check if mostly printable base64 chars
            var base64Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
            return value.All(c => base64Chars.Contains(c));
        }
        catch
        {
            return false;
        }
    }

    private bool IsHex(string value)
    {
        return value.All(c => "0123456789ABCDEFabcdef".Contains(c));
    }
}

/// <summary>
/// Image type metadata.
/// </summary>
public sealed record ImageType(string Extension, string MimeType);

/// <summary>
/// Conversion result with metadata.
/// </summary>
public sealed class AjisConversionResult
{
    public required string SourceFile { get; init; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public long OriginalSize { get; set; }
    public long AjisSize { get; set; }
    public double SizeReduction { get; set; }
    public int BinaryAttachmentsDetected { get; set; }
    public string? AjisText { get; set; }
    public List<(string Path, BinaryAttachment Attachment)>? DetectedAttachments { get; set; }
    public AtpMetadata? AtpMetadata { get; set; }
}

/// <summary>
/// ATP file format wrapper.
/// </summary>
[Serializable]
public sealed class AtpFileWrapper
{
    [JsonPropertyName("ajisContent")]
    public string AjisContent { get; set; } = "";

    [JsonPropertyName("metadata")]
    public AtpMetadata Metadata { get; set; } = new();

    [JsonPropertyName("attachments")]
    public List<(string Path, BinaryAttachment Attachment)> Attachments { get; set; } = new();
}

/// <summary>
/// ATP metadata.
/// </summary>
[Serializable]
public sealed class AtpMetadata
{
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("sourceFormat")]
    public string SourceFormat { get; set; } = "JSON";

    [JsonPropertyName("conversionMode")]
    public string ConversionMode { get; set; } = "Auto-ATP";

    [JsonPropertyName("binaryAttachmentCount")]
    public int BinaryAttachmentCount { get; set; }

    [JsonPropertyName("binaryAttachmentData")]
    public List<(string Path, BinaryAttachment Attachment)> BinaryAttachmentData { get; set; } = new();

    [JsonPropertyName("originalSize")]
    public long OriginalSize { get; set; }

    [JsonPropertyName("ajisSize")]
    public long AjisSize { get; set; }

    [JsonPropertyName("sizeReduction")]
    public double SizeReduction { get; set; }
}

/// <summary>
/// Conversion options.
/// </summary>
public sealed class ConversionOptions
{
    /// <summary>
    /// Enable automatic binary detection and extraction.
    /// Default: true
    /// </summary>
    public bool EnableBinaryDetection { get; set; } = true;

    /// <summary>
    /// Minimum string length to check for binary.
    /// Default: 20
    /// </summary>
    public int MinimumBinaryLength { get; set; } = 20;

    /// <summary>
    /// Save extracted binaries as separate files.
    /// Default: false (embedded in ATP)
    /// </summary>
    public bool SaveBinariesSeparately { get; set; } = false;

    /// <summary>
    /// Output directory for separate binary files.
    /// </summary>
    public string? BinaryOutputDirectory { get; set; }

    /// <summary>
    /// Compress binaries automatically.
    /// Default: true
    /// </summary>
    public bool CompressBinaries { get; set; } = true;
}
