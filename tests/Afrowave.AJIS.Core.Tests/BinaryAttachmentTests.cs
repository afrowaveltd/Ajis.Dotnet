#nullable enable

using Afrowave.AJIS.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Afrowave.AJIS.Core.Tests;

/// <summary>
/// ATP (Attachment Transfer Protocol) Tests
/// </summary>
public sealed class BinaryAttachmentTests
{
    [Fact]
    public void CreateAttachment_WithValidData_Succeeds()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Hello, World!");
        var attachment = new BinaryAttachment
        {
            FileName = "test.txt",
            MimeType = "text/plain",
            Data = data
        };

        // Act
        attachment.ComputeChecksum();

        // Assert
        Assert.NotEmpty(attachment.Checksum);
        Assert.Equal(data.Length, attachment.FileSize);
        Assert.Equal(data.Length, attachment.OriginalSize);
    }

    [Fact]
    public void VerifyChecksum_WithValidData_Succeeds()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Test data");
        var attachment = new BinaryAttachment
        {
            FileName = "test.txt",
            Data = data
        };
        attachment.ComputeChecksum();

        // Act
        var isValid = attachment.VerifyChecksum();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyChecksum_WithModifiedData_Fails()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Original data");
        var attachment = new BinaryAttachment
        {
            FileName = "test.txt",
            Data = data
        };
        attachment.ComputeChecksum();

        // Act
        attachment.Data = Encoding.UTF8.GetBytes("Modified data");
        var isValid = attachment.VerifyChecksum();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void GetFormattedSize_WithLargeFile_FormatsCorrectly()
    {
        // Arrange
        var attachment = new BinaryAttachment
        {
            FileSize = 1024 * 1024  // 1 MB
        };

        // Act
        var formatted = attachment.GetFormattedSize();

        // Assert
        Assert.Contains("MB", formatted);
    }

    [Fact]
    public void GetCompressionRatio_WithCompression_CalculatesCorrectly()
    {
        // Arrange
        var attachment = new BinaryAttachment
        {
            OriginalSize = 1000,
            FileSize = 400  // 40% of original
        };

        // Act
        var ratio = attachment.GetCompressionRatio();

        // Assert
        Assert.True(ratio > 59 && ratio < 61);  // ~60%
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new BinaryAttachment
        {
            FileName = "original.txt",
            Data = Encoding.UTF8.GetBytes("data"),
            Metadata = new() { { "key", "value" } }
        };

        // Act
        var clone = original.Clone();
        clone.FileName = "clone.txt";
        clone.Metadata["key"] = "modified";

        // Assert
        Assert.Equal("original.txt", original.FileName);
        Assert.Equal("value", (string)original.Metadata["key"]);
    }

    [Fact]
    public void AttachmentValidator_WithValidData_Passes()
    {
        // Arrange
        var validator = new AttachmentValidator(
            maxFileSize: 1024 * 1024,  // 1MB
            allowedMimeTypes: new[] { "application/pdf", "image/*" }
        );

        var attachment = new BinaryAttachment
        {
            FileName = "test.pdf",
            MimeType = "application/pdf",
            Data = new byte[1000]
        };
        attachment.ComputeChecksum();

        // Act
        var (isValid, error) = validator.Validate(attachment);

        // Assert
        Assert.True(isValid);
        Assert.Empty(error);
    }

    [Fact]
    public void AttachmentValidator_ExceedsMaxFileSize_Fails()
    {
        // Arrange
        var validator = new AttachmentValidator(maxFileSize: 1000);

        var attachment = new BinaryAttachment
        {
            FileName = "large.bin",
            MimeType = "application/octet-stream",
            Data = new byte[10000],
            FileSize = 10000
        };

        // Act
        var (isValid, error) = validator.Validate(attachment);

        // Assert
        Assert.False(isValid);
        Assert.Contains("exceeds maximum", error);
    }

    [Fact]
    public void AttachmentValidator_InvalidMimeType_Fails()
    {
        // Arrange
        var validator = new AttachmentValidator(
            allowedMimeTypes: new[] { "image/*" }
        );

        var attachment = new BinaryAttachment
        {
            FileName = "test.pdf",
            MimeType = "application/pdf",
            Data = new byte[1000]
        };

        // Act
        var (isValid, error) = validator.Validate(attachment);

        // Assert
        Assert.False(isValid);
        Assert.Contains("not allowed", error);
    }

    [Fact]
    public void AttachmentValidator_WildcardMimeType_Matches()
    {
        // Arrange
        var validator = new AttachmentValidator(
            allowedMimeTypes: new[] { "image/*" }
        );

        var attachment = new BinaryAttachment
        {
            FileName = "photo.jpg",
            MimeType = "image/jpeg",
            Data = new byte[1000]
        };

        // Act
        var (isValid, error) = validator.Validate(attachment);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void GetMimeType_WithCommonExtensions_ReturnsCorrectType()
    {
        // Act & Assert
        Assert.Equal("application/pdf", AjisAttachmentHelper.GetMimeType(".pdf"));
        Assert.Equal("image/png", AjisAttachmentHelper.GetMimeType(".png"));
        Assert.Equal("image/jpeg", AjisAttachmentHelper.GetMimeType(".jpg"));
        Assert.Equal("video/mp4", AjisAttachmentHelper.GetMimeType(".mp4"));
        Assert.Equal("text/plain", AjisAttachmentHelper.GetMimeType(".txt"));
    }

    [Fact]
    public void EstimateCompressionRatio_ForTextFiles_ReturnsHighRatio()
    {
        // Act
        var ratio = AjisAttachmentHelper.EstimateCompressionRatio("text/plain");

        // Assert
        Assert.True(ratio > 0.2);  // Text compresses well
    }

    [Fact]
    public void EstimateCompressionRatio_ForImageFiles_ReturnsLowRatio()
    {
        // Act
        var ratio = AjisAttachmentHelper.EstimateCompressionRatio("image/jpeg");

        // Assert
        Assert.True(ratio < 0.1);  // Images already compressed
    }

    [Fact]
    public void CreateFromFile_WithValidFile_CreatesAttachment()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var testData = Encoding.UTF8.GetBytes("Test file content");
        File.WriteAllBytes(tempFile, testData);

        try
        {
            // Act
            var attachment = AjisAttachmentHelper.CreateFromFile(tempFile);

            // Assert
            Assert.NotNull(attachment);
            Assert.Equal(Path.GetFileName(tempFile), attachment.FileName);
            Assert.NotEmpty(attachment.Checksum);
            Assert.Equal(testData.Length, attachment.FileSize);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveToFile_WithValidAttachment_SavesSuccessfully()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var testData = Encoding.UTF8.GetBytes("Save test content");
        var attachment = new BinaryAttachment
        {
            FileName = "test.txt",
            Data = testData
        };

        try
        {
            // Act
            AjisAttachmentHelper.SaveToFile(attachment, tempFile);

            // Assert
            var savedData = File.ReadAllBytes(tempFile);
            Assert.Equal(testData, savedData);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void AttachmentAttribute_WithDefaultValues_HasCorrectDefaults()
    {
        // Arrange & Act
        var attr = new AjisAttachmentAttribute();

        // Assert
        Assert.True(attr.AutoCompress);
        Assert.Equal(0, attr.MaxFileSize);
        Assert.Null(attr.AllowedMimeTypes);
        Assert.True(attr.VerifyChecksum);
        Assert.False(attr.Encrypt);
    }

    [Fact]
    public void MultipleAttachments_InCollection_AllHaveUniqueIds()
    {
        // Arrange
        var attachments = new List<BinaryAttachment>
        {
            new() { FileName = "file1.txt", Data = new byte[] { 1, 2, 3 } },
            new() { FileName = "file2.txt", Data = new byte[] { 4, 5, 6 } },
            new() { FileName = "file3.txt", Data = new byte[] { 7, 8, 9 } }
        };

        // Act
        var ids = new HashSet<Guid>();
        foreach (var att in attachments)
        {
            ids.Add(att.AttachmentId);
        }

        // Assert
        Assert.Equal(3, ids.Count);  // All unique
    }

    [Fact]
    public void AttachmentMetadata_StoresArbitraryData()
    {
        // Arrange
        var attachment = new BinaryAttachment
        {
            FileName = "image.jpg",
            Data = new byte[1000],
            Metadata = new()
            {
                { "width", 1920 },
                { "height", 1080 },
                { "camera", "Canon EOS" },
                { "iso", 400 }
            }
        };

        // Act & Assert
        Assert.Equal(1920, (int)attachment.Metadata["width"]);
        Assert.Equal("Canon EOS", (string)attachment.Metadata["camera"]);
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    public void GetFormattedSize_WithVariousSizes_FormatsCorrectly(long size, string expected)
    {
        // Arrange
        var attachment = new BinaryAttachment { FileSize = size };

        // Act
        var formatted = attachment.GetFormattedSize();

        // Assert
        Assert.Contains(expected, formatted);
    }
}
