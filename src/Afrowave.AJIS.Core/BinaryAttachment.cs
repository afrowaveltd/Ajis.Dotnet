#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Afrowave.AJIS.Core;

/// <summary>
/// Represents a binary attachment in AJIS (ATP - Attachment Transfer Protocol).
/// Enables seamless embedding of binary files in AJIS documents with compression and validation.
/// </summary>
public class BinaryAttachment
{
    /// <summary>
    /// Unique identifier for this attachment.
    /// </summary>
    public Guid AttachmentId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Original file name with extension.
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    /// MIME type (e.g., "application/pdf", "image/png", "video/mp4").
    /// </summary>
    public string MimeType { get; set; } = "";

    /// <summary>
    /// Binary file data.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// File size in bytes (original, before compression).
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// SHA256 checksum for integrity verification.
    /// </summary>
    public string Checksum { get; set; } = "";

    /// <summary>
    /// Compression type: 0=none, 1=gzip, 2=brotli.
    /// </summary>
    public int CompressionType { get; set; } = 0;

    /// <summary>
    /// Original size before compression.
    /// </summary>
    public long OriginalSize { get; set; }

    /// <summary>
    /// Creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata (optional).
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Computes SHA256 checksum for this attachment.
    /// </summary>
    public void ComputeChecksum()
    {
        if (Data == null || Data.Length == 0)
        {
            Checksum = "";
            return;
        }

        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Data);
            Checksum = BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        FileSize = Data.Length;
        OriginalSize = Data.Length;
    }

    /// <summary>
    /// Verifies attachment integrity using stored checksum.
    /// </summary>
    public bool VerifyChecksum()
    {
        if (string.IsNullOrEmpty(Checksum) || Data == null)
            return false;

        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Data);
            var computed = BitConverter.ToString(hash).Replace("-", "").ToLower();
            return computed == Checksum.ToLower();
        }
    }

    /// <summary>
    /// Gets a human-readable size string.
    /// </summary>
    public string GetFormattedSize()
    {
        return FormatBytes(FileSize);
    }

    /// <summary>
    /// Gets compression ratio as percentage.
    /// </summary>
    public double GetCompressionRatio()
    {
        if (OriginalSize <= 0)
            return 0;

        return (1.0 - (double)FileSize / OriginalSize) * 100;
    }

    /// <summary>
    /// Creates a copy of this attachment.
    /// </summary>
    public BinaryAttachment Clone()
    {
        return new BinaryAttachment
        {
            AttachmentId = AttachmentId,
            FileName = FileName,
            MimeType = MimeType,
            Data = (byte[])Data.Clone(),
            FileSize = FileSize,
            Checksum = Checksum,
            CompressionType = CompressionType,
            OriginalSize = OriginalSize,
            CreatedDate = CreatedDate,
            Metadata = new Dictionary<string, object>(Metadata)
        };
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Attribute to mark a property as an AJIS binary attachment (ATP protocol).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AjisAttachmentAttribute : Attribute
{
    /// <summary>
    /// Enable automatic compression for attachments.
    /// Default: true (gzip for small files, brotli for large).
    /// </summary>
    public bool AutoCompress { get; set; } = true;

    /// <summary>
    /// Maximum file size in bytes. 0 = unlimited.
    /// Default: 0 (no limit).
    /// </summary>
    public long MaxFileSize { get; set; } = 0;

    /// <summary>
    /// Allowed MIME types (e.g., "application/pdf", "image/*").
    /// null = allow any MIME type.
    /// </summary>
    public string[]? AllowedMimeTypes { get; set; }

    /// <summary>
    /// Enable checksum verification on deserialization.
    /// Default: true.
    /// </summary>
    public bool VerifyChecksum { get; set; } = true;

    /// <summary>
    /// Enable encryption for this attachment (future feature).
    /// Default: false.
    /// </summary>
    public bool Encrypt { get; set; } = false;
}

/// <summary>
/// Attachment validator for ATP (Attachment Transfer Protocol).
/// </summary>
public class AttachmentValidator
{
    private readonly long _maxFileSize;
    private readonly string[]? _allowedMimeTypes;
    private readonly bool _verifyChecksum;

    public AttachmentValidator(
        long maxFileSize = 0,
        string[]? allowedMimeTypes = null,
        bool verifyChecksum = true)
    {
        _maxFileSize = maxFileSize;
        _allowedMimeTypes = allowedMimeTypes;
        _verifyChecksum = verifyChecksum;
    }

    /// <summary>
    /// Validates an attachment.
    /// </summary>
    public (bool IsValid, string Error) Validate(BinaryAttachment attachment)
    {
        if (attachment == null)
            return (false, "Attachment is null");

        if (string.IsNullOrEmpty(attachment.FileName))
            return (false, "File name is required");

        if (attachment.Data == null || attachment.Data.Length == 0)
            return (false, "File data is empty");

        // Check file size
        if (_maxFileSize > 0 && attachment.FileSize > _maxFileSize)
            return (false, $"File size {attachment.FileSize} exceeds maximum {_maxFileSize}");

        // Check MIME type
        if (_allowedMimeTypes != null && _allowedMimeTypes.Length > 0)
        {
            if (!IsAllowedMimeType(attachment.MimeType))
                return (false, $"MIME type '{attachment.MimeType}' not allowed");
        }

        // Verify checksum
        if (_verifyChecksum && !string.IsNullOrEmpty(attachment.Checksum))
        {
            if (!attachment.VerifyChecksum())
                return (false, "Checksum verification failed");
        }

        return (true, "");
    }

    private bool IsAllowedMimeType(string mimeType)
    {
        if (_allowedMimeTypes == null)
            return true;

        foreach (var allowed in _allowedMimeTypes)
        {
            if (allowed.EndsWith("*"))
            {
                // Wildcard match (e.g., "image/*")
                var prefix = allowed.Substring(0, allowed.Length - 1);
                if (mimeType.StartsWith(prefix))
                    return true;
            }
            else if (mimeType == allowed)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// ATP (Attachment Transfer Protocol) helper methods.
/// </summary>
public static class AjisAttachmentHelper
{
    /// <summary>
    /// Creates a binary attachment from a file path.
    /// </summary>
    public static BinaryAttachment CreateFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var fileInfo = new FileInfo(filePath);
        var data = File.ReadAllBytes(filePath);

        var attachment = new BinaryAttachment
        {
            FileName = fileInfo.Name,
            MimeType = GetMimeType(fileInfo.Extension),
            Data = data,
            FileSize = data.Length,
            OriginalSize = data.Length
        };

        attachment.ComputeChecksum();
        return attachment;
    }

    /// <summary>
    /// Saves attachment to disk.
    /// </summary>
    public static void SaveToFile(BinaryAttachment attachment, string outputPath)
    {
        if (attachment?.Data == null)
            throw new ArgumentException("Attachment data is null");

        File.WriteAllBytes(outputPath, attachment.Data);
    }

    /// <summary>
    /// Gets MIME type from file extension.
    /// </summary>
    public static string GetMimeType(string extension)
    {
        return extension?.ToLower() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".mpeg" => "video/mpeg",
            ".webm" => "video/webm",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".zip" => "application/zip",
            ".gz" => "application/gzip",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Estimates compression ratio for a file type.
    /// </summary>
    public static double EstimateCompressionRatio(string mimeType)
    {
        // Already compressed formats
        if (mimeType?.StartsWith("image/") == true ||
            mimeType?.StartsWith("video/") == true ||
            mimeType?.Contains("zip") == true)
            return 0.05;  // 5% compression ratio (not worth it)

        // Text formats compress well
        if (mimeType?.StartsWith("text/") == true ||
            mimeType?.Contains("json") == true ||
            mimeType?.Contains("xml") == true)
            return 0.30;  // 30% compression ratio

        // Default
        return 0.40;  // 40% compression ratio
    }
}
