#nullable enable

using Afrowave.AJIS.IO;
using Xunit;

namespace Afrowave.AJIS.IO.Tests;

public sealed class AjisFileReaderWriterTests : IAsyncLifetime
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), $"AjisFileTests_{Guid.NewGuid():N}");

    public async Task InitializeAsync()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
        Directory.CreateDirectory(_testDirectory);
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
        await Task.CompletedTask;
    }

    // Helper: generate unique file name for each test
    private string UniqueFile(string baseName)
        => Path.Combine(_testDirectory, $"{baseName}_{Guid.NewGuid():N}.ajis");

    // ===== M8A File I/O Tests =====

    [Fact]
    public async Task FileWriter_CreatesNewFile()
    {
        var filePath = UniqueFile("test");
        await using (var writer = new AjisFileWriter(filePath))
        {
            await writer.WriteAsync("[");
            await writer.WriteAsync("{\"id\":1,\"name\":\"Alice\"}");
            await writer.WriteAsync("]");
        }
        Assert.True(File.Exists(filePath));
        var content = await File.ReadAllTextAsync(filePath);
        Assert.Contains("Alice", content);
    }

    [Fact]
    public async Task FileWriter_WritesContent()
    {
        var filePath = UniqueFile("content");
        var testContent = "{\"test\":\"value\"}";
        await using (var writer = new AjisFileWriter(filePath))
        {
            await writer.WriteAsync(testContent);
        }
        var written = await File.ReadAllTextAsync(filePath);
        Assert.Equal(testContent, written);
    }

    [Fact]
    public async Task FileWriter_FlushesContent()
    {
        var filePath = UniqueFile("flush");
        await using (var writer = new AjisFileWriter(filePath))
        {
            await writer.WriteAsync("test");
            await writer.FlushAsync();
        }
        Assert.True(File.Exists(filePath));
        var content = await File.ReadAllTextAsync(filePath);
        Assert.Contains("test", content);
    }

    [Fact]
    public async Task FileWriter_Finalizes()
    {
        var filePath = UniqueFile("finalize");
        var writer = new AjisFileWriter(filePath);
        await writer.WriteAsync("content");
        await writer.FinalizeAsync();
        Assert.True(writer.IsFinalized);
        await writer.DisposeAsync();
    }

    [Fact]
    public async Task FileWriter_AppendsToFile()
    {
        var filePath = UniqueFile("append");
        await using (var writer = new AjisFileWriter(filePath))
        {
            await writer.WriteAsync("first");
        }
        await using (var writer = new AjisFileWriter(filePath, FileMode.Append))
        {
            await writer.WriteAsync("second");
        }
        var content = await File.ReadAllTextAsync(filePath);
        Assert.Equal("firstsecond", content);
    }

    [Fact]
    public async Task FileWriter_CreatesDirectories()
    {
        var filePath = Path.Combine(_testDirectory, $"subdir_{Guid.NewGuid():N}", "nested", "file.ajis");
        await using (var writer = new AjisFileWriter(filePath))
        {
            await writer.WriteAsync("content");
        }
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task FileReader_OpensExistingFile()
    {
        var filePath = UniqueFile("read");
        var testContent = "[1, 2, 3]";
        await File.WriteAllTextAsync(filePath, testContent);
        using (var reader = new AjisFileReader(filePath))
        {
            Assert.Equal(filePath, reader.FilePath);
            Assert.True(reader.FileSize > 0);
            Assert.False(reader.IsAtEnd);
        }
    }

    [Fact]
    public void FileReader_ThrowsOnMissingFile()
    {
        var filePath = UniqueFile("nonexistent");
        Assert.Throws<FileNotFoundException>(() => new AjisFileReader(filePath));
    }

    [Fact]
    public void FileReader_ReadsAsStream()
    {
        var filePath = UniqueFile("stream");
        var testContent = "{\"items\": [1, 2, 3]}";
        File.WriteAllText(filePath, testContent);
        using (var reader = new AjisFileReader(filePath))
        {
            var stream = reader.OpenAsStream();
            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
            stream.Dispose();
        }
    }

    [Fact]
    public void FileReader_Seeks()
    {
        var filePath = UniqueFile("seek");
        var testContent = "0123456789";
        File.WriteAllText(filePath, testContent);
        using (var reader = new AjisFileReader(filePath))
        {
            reader.Seek(5);
            Assert.Equal(5, reader.CurrentPosition);
            reader.Reset();
            Assert.Equal(0, reader.CurrentPosition);
        }
    }

    [Fact]
    public void FileReader_RejectsInvalidSeek()
    {
        var filePath = UniqueFile("seek_invalid");
        File.WriteAllText(filePath, "test");
        using (var reader = new AjisFileReader(filePath))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.Seek(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.Seek(10000));
        }
    }

    [Fact]
    public void FileReader_ThrowsWhenDisposed()
    {
        var filePath = UniqueFile("disposed");
        File.WriteAllText(filePath, "test");
        var reader = new AjisFileReader(filePath);
        reader.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = reader.FileSize);
    }

    [Fact]
    public async Task FileWriter_ThrowsWhenDisposed()
    {
        var filePath = UniqueFile("disposed_writer");
        var writer = new AjisFileWriter(filePath);
        await writer.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => writer.WriteAsync("test"));
    }

    [Fact]
    public async Task FileReader_HandlesLargeFile()
    {
        var filePath = UniqueFile("large");
        var largeContent = new string('x', 1_000_000); // 1MB
        await File.WriteAllTextAsync(filePath, largeContent);
        using (var reader = new AjisFileReader(filePath))
        {
            Assert.Equal(1_000_000, reader.FileSize);
        }
    }
}
