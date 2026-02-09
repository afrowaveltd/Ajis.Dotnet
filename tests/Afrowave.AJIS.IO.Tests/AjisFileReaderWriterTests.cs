#nullable enable

using Afrowave.AJIS.IO;
using Xunit;

namespace Afrowave.AJIS.IO.Tests;

public sealed class AjisFileReaderWriterTests : IAsyncLifetime
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "AjisFileTests");

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

    // ===== M8A File I/O Tests =====

    [Fact]
    public async Task FileWriter_CreatesNewFile()
    {
        var filePath = Path.Combine(_testDirectory, "test.ajis");
        
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
        var filePath = Path.Combine(_testDirectory, "content.ajis");
        var testContent = "{\"test\":\"value\"}";

        await using (var writer = new AjisFileWriter(filePath))
        {
            await writer.WriteAsync(testContent);
        }

        var written = await File.ReadAllTextAsync(filePath);
        Assert.Equal(testContent, written);
    }

    [Fact]
    public async Task FileReader_OpensExistingFile()
    {
        var filePath = Path.Combine(_testDirectory, "read.ajis");
        var testContent = "[1, 2, 3]";
        await File.WriteAllTextAsync(filePath, testContent);

        using (var reader = new AjisFileReader(filePath))
        {
            Assert.Equal(filePath, reader.FilePath);
            Assert.Greater(reader.FileSize, 0);
            Assert.False(reader.IsAtEnd);
        }
    }

    [Fact]
    public void FileReader_ThrowsOnMissingFile()
    {
        var filePath = Path.Combine(_testDirectory, "nonexistent.ajis");

        Assert.Throws<FileNotFoundException>(() => new AjisFileReader(filePath));
    }

    [Fact]
    public void FileReader_ReadsAsStream()
    {
        var filePath = Path.Combine(_testDirectory, "stream.ajis");
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
    public async Task FileWriter_FlushesContent()
    {
        var filePath = Path.Combine(_testDirectory, "flush.ajis");
        var writer = new AjisFileWriter(filePath);
        
        await writer.WriteAsync("test");
        await writer.FlushAsync();
        
        // File should exist and contain content even before dispose
        Assert.True(File.Exists(filePath));
        var content = await File.ReadAllTextAsync(filePath);
        Assert.Contains("test", content);
        
        await writer.DisposeAsync();
    }

    [Fact]
    public async Task FileWriter_Finalizes()
    {
        var filePath = Path.Combine(_testDirectory, "finalize.ajis");
        var writer = new AjisFileWriter(filePath);
        
        await writer.WriteAsync("content");
        await writer.FinalizeAsync();
        
        Assert.True(writer.IsFinalized);
        await writer.DisposeAsync();
    }

    [Fact]
    public void FileReader_Seeks()
    {
        var filePath = Path.Combine(_testDirectory, "seek.ajis");
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
        var filePath = Path.Combine(_testDirectory, "seek_invalid.ajis");
        File.WriteAllText(filePath, "test");

        using (var reader = new AjisFileReader(filePath))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.Seek(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.Seek(10000));
        }
    }

    [Fact]
    public async Task FileWriter_AppendsToFile()
    {
        var filePath = Path.Combine(_testDirectory, "append.ajis");
        
        // Create initial file
        await using (var writer = new AjisFileWriter(filePath))
        {
            await writer.WriteAsync("first");
        }

        // Append to existing file
        await using (var writer = new AjisFileWriter(filePath, FileMode.Append))
        {
            await writer.WriteAsync("second");
        }

        var content = await File.ReadAllTextAsync(filePath);
        Assert.Equal("firstsecond", content);
    }

    [Fact]
    public void FileReader_ThrowsWhenDisposed()
    {
        var filePath = Path.Combine(_testDirectory, "disposed.ajis");
        File.WriteAllText(filePath, "test");

        var reader = new AjisFileReader(filePath);
        reader.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = reader.FileSize);
    }

    [Fact]
    public async Task FileWriter_ThrowsWhenDisposed()
    {
        var filePath = Path.Combine(_testDirectory, "disposed_writer.ajis");
        var writer = new AjisFileWriter(filePath);
        await writer.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => writer.WriteAsync("test"));
    }

    [Fact]
    public async Task FileWriter_CreatesDirectories()
    {
        var filePath = Path.Combine(_testDirectory, "subdir", "nested", "file.ajis");
        
        await using (var writer = new AjisFileWriter(filePath))
        {
            await writer.WriteAsync("content");
        }

        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task FileReader_HandlesLargeFile()
    {
        var filePath = Path.Combine(_testDirectory, "large.ajis");
        var largeContent = new string('x', 1_000_000); // 1MB
        await File.WriteAllTextAsync(filePath, largeContent);

        using (var reader = new AjisFileReader(filePath))
        {
            Assert.Equal(1_000_000, reader.FileSize);
        }
    }
}
