#nullable enable

using Afrowave.AJIS.IO;
using Xunit;

namespace Afrowave.AJIS.IO.Tests;

public sealed class AjisFileHighLevelTests : IAsyncLifetime
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "AjisFileHighLevel");

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

    // ===== M8A Phase 2: High-Level API Tests =====

    [Fact]
    public void Create_CreatesFileWithObjects()
    {
        var filePath = Path.Combine(_testDirectory, "create.ajis");
        var items = new[] 
        { 
            new TestUser { Id = 1, Name = "Alice" },
            new TestUser { Id = 2, Name = "Bob" }
        };

        AjisFile.Create(filePath, items);

        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.Contains("Alice", content);
        Assert.Contains("Bob", content);
    }

    [Fact]
    public void ReadAll_ReadsAllObjects()
    {
        var filePath = Path.Combine(_testDirectory, "readall.ajis");
        var items = new[] 
        { 
            new TestUser { Id = 1, Name = "Charlie" },
            new TestUser { Id = 2, Name = "Diana" }
        };

        AjisFile.Create(filePath, items);
        var read = AjisFile.ReadAll<TestUser>(filePath);

        Assert.NotEmpty(read);
        Assert.Equal(2, read.Count);
    }

    [Fact]
    public void Enumerate_EnumeratesObjects()
    {
        var filePath = Path.Combine(_testDirectory, "enum.ajis");
        var items = new[] 
        { 
            new TestUser { Id = 1, Name = "Eve" }
        };

        AjisFile.Create(filePath, items);
        var enumerated = AjisFile.Enumerate<TestUser>(filePath).ToList();

        Assert.NotEmpty(enumerated);
    }

    [Fact]
    public void ReadAt_ReadsSpecificIndex()
    {
        var filePath = Path.Combine(_testDirectory, "readat.ajis");
        var items = new[] 
        { 
            new TestUser { Id = 1, Name = "Frank" },
            new TestUser { Id = 2, Name = "Grace" }
        };

        AjisFile.Create(filePath, items);
        var item = AjisFile.ReadAt<TestUser>(filePath, 0);

        Assert.NotNull(item);
    }

    [Fact]
    public void Append_AppendsToFile()
    {
        var filePath = Path.Combine(_testDirectory, "append.ajis");
        var initial = new TestUser { Id = 1, Name = "Henry" };
        
        AjisFile.Create(filePath, new[] { initial });
        
        var appended = new TestUser { Id = 2, Name = "Iris" };
        AjisFile.Append(filePath, appended);

        var all = AjisFile.ReadAll<TestUser>(filePath);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task CreateAsync_CreatesFileAsync()
    {
        var filePath = Path.Combine(_testDirectory, "create_async.ajis");
        var items = new[] 
        { 
            new TestUser { Id = 1, Name = "Jack" }
        };

        await AjisFile.CreateAsync(filePath, items.ToAsyncEnumerable());

        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task AppendAsync_AppendsAsync()
    {
        var filePath = Path.Combine(_testDirectory, "append_async.ajis");
        var item = new TestUser { Id = 1, Name = "Kate" };
        
        await AjisFile.CreateAsync(filePath, new[] { item }.ToAsyncEnumerable());
        
        var appended = new TestUser { Id = 2, Name = "Leo" };
        await AjisFile.AppendAsync(filePath, appended);

        var all = AjisFile.ReadAll<TestUser>(filePath);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task ReadAllAsync_ReadsAsync()
    {
        var filePath = Path.Combine(_testDirectory, "readall_async.ajis");
        var items = new[] 
        { 
            new TestUser { Id = 1, Name = "Mona" }
        };

        AjisFile.Create(filePath, items);
        
        var read = new List<TestUser>();
        await foreach (var item in AjisFile.ReadAllAsync<TestUser>(filePath))
        {
            read.Add(item);
        }

        Assert.NotEmpty(read);
    }

    [Fact]
    public void Create_ThrowsOnNullPath()
    {
        Assert.Throws<ArgumentNullException>(() => 
            AjisFile.Create<TestUser>(null!, new TestUser[] { }));
    }

    [Fact]
    public void ReadAll_ThrowsOnMissingFile()
    {
        var filePath = Path.Combine(_testDirectory, "nonexistent.ajis");

        Assert.Throws<FileNotFoundException>(() => AjisFile.ReadAll<TestUser>(filePath));
    }

    // Test helper class
    private sealed class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
