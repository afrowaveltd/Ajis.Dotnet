#nullable enable

using Afrowave.AJIS.IO;
using Afrowave.AJIS.Serialization.Mapping;
using Xunit;

namespace Afrowave.AJIS.IO.Tests;

public class AjisIndexTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "AjisIndexTests");

    public AjisIndexTests()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    [Fact]
    public async Task AjisIndex_BuildAndFind_FindsItem()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.ajis");
        using var context = new AjisContext();
        var users = context.Set<User>(filePath);

        await users.AddAsync(new User { Id = 1, Name = "Alice" });
        await users.AddAsync(new User { Id = 2, Name = "Bob" });
        await context.SaveChangesAsync();

        var index = new AjisIndex<User>(filePath, "Name");
        await index.BuildAsync();

        // Act
        var found = await index.FindAsync("Bob");

        // Assert
        Assert.NotNull(found);
        Assert.Equal(2, found?.Id);
    }

    [Fact]
    public async Task AjisIndex_FindAll_ReturnsMultiple()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.ajis");
        var context = new AjisContext();
        var users = context.Set<User>(filePath);

        await users.AddAsync(new User { Id = 1, Name = "Alice", Age = 25 });
        await users.AddAsync(new User { Id = 2, Name = "Bob", Age = 30 });
        await users.AddAsync(new User { Id = 3, Name = "Charlie", Age = 25 });
        await context.SaveChangesAsync();

        var index = new AjisIndex<User>(filePath, "Age");
        await index.BuildAsync();

        // Act
        var found = await index.FindAllAsync(25);

        // Assert
        Assert.Equal(2, found.Count());
        var names = found.Select(u => u?.Name).OrderBy(n => n).ToList();
        Assert.Contains("Alice", names);
        Assert.Contains("Charlie", names);
    }

    [Fact]
    public async Task AjisIndex_Contains_ReturnsCorrectValue()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.ajis");
        var context = new AjisContext();
        var users = context.Set<User>(filePath);

        await users.AddAsync(new User { Id = 1, Name = "Alice" });
        await users.AddAsync(new User { Id = 2, Name = "Bob" });
        await context.SaveChangesAsync();

        var index = new AjisIndex<User>(filePath, "Name");
        await index.BuildAsync();

        // Act & Assert
        Assert.True(await index.ContainsAsync("Alice"));
        Assert.False(await index.ContainsAsync("Charlie"));
    }

    [Fact]
    public async Task AjisIndex_GetValues_ReturnsUniqueValues()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.ajis");
        var context = new AjisContext();
        var users = context.Set<User>(filePath);

        await users.AddAsync(new User { Id = 1, Name = "Alice" });
        await users.AddAsync(new User { Id = 2, Name = "Bob" });
        await users.AddAsync(new User { Id = 3, Name = "Charlie" });
        await context.SaveChangesAsync();

        var index = new AjisIndex<User>(filePath, "Name");
        await index.BuildAsync();

        // Act
        var values = await index.GetValuesAsync();
        var valueList = values.OrderBy(v => v).ToList();

        // Assert
        Assert.Equal(3, valueList.Count);
        Assert.Equal("Alice", valueList[0]);
        Assert.Equal("Bob", valueList[1]);
        Assert.Equal("Charlie", valueList[2]);
    }

    [Fact]
    public async Task AjisIndex_Cache_UsesMemoryEfficiently()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.ajis");

        // Create file with 1000 users
        var users = Enumerable.Range(1, 1000)
            .Select(i => new User { Id = i, Name = $"User{i}" });
        AjisFile.Create(filePath, users);

        var index = new AjisIndex<User>(filePath, "Name");
        
        // Act - Build index (should cache only the indexed values)
        await index.BuildAsync();

        // Assert - Index should use HashDictionary, not store all items
        Assert.Equal(1000, index.Count);
    }

    [Fact]
    public async Task AjisIndex_Reload_FreshData()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.ajis");
        var context = new AjisContext();
        var users = context.Set<User>(filePath);

        await users.AddAsync(new User { Id = 1, Name = "Alice" });
        await context.SaveChangesAsync();

        var index = new AjisIndex<User>(filePath, "Name");
        await index.BuildAsync();
        Assert.True(await index.ContainsAsync("Alice"));

        // Add new user
        await users.AddAsync(new User { Id = 2, Name = "Bob" });
        await context.SaveChangesAsync();

        // Act - Reload index
        await index.ReloadAsync();

        // Assert
        Assert.True(await index.ContainsAsync("Alice"));
        Assert.True(await index.ContainsAsync("Bob"));
        Assert.Equal(2, index.Count);
    }

    [Fact]
    public async Task AjisSet_CreateIndex_Extension()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.ajis");
        using var context = new AjisContext();
        var users = context.Set<User>(filePath);

        await users.AddAsync(new User { Id = 1, Name = "Alice" });
        await users.AddAsync(new User { Id = 2, Name = "Bob" });
        await context.SaveChangesAsync();

        // Act - Create index using extension method
        var index = users.CreateIndex("Name");
        await index.BuildAsync();

        // Assert
        var found = await index.FindAsync("Bob");
        Assert.NotNull(found);
        Assert.Equal(2, found?.Id);
    }

    [Fact]
    public async Task AjisIndex_Find_SetsDefaultValue_WhenNotFound()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.ajis");
        var context = new AjisContext();
        var users = context.Set<User>(filePath);

        await users.AddAsync(new User { Id = 1, Name = "Alice" });
        await context.SaveChangesAsync();

        var index = new AjisIndex<User>(filePath, "Name");
        await index.BuildAsync();

        // Act
        var notFound = await index.FindAsync("Charlie");

        // Assert
        Assert.Null(notFound);
    }

    [Fact]
    public async Task AjisIndex_MultipleIndexes_CanCoexist()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.ajis");
        var context = new AjisContext();
        var users = context.Set<User>(filePath);

        await users.AddAsync(new User { Id = 1, Name = "Alice", Age = 25 });
        await users.AddAsync(new User { Id = 2, Name = "Bob", Age = 30 });
        await context.SaveChangesAsync();

        var nameIndex = new AjisIndex<User>(filePath, "Name");
        var ageIndex = new AjisIndex<User>(filePath, "Age");

        await nameIndex.BuildAsync();
        await ageIndex.BuildAsync();

        // Act & Assert
        Assert.Equal(2, nameIndex.Count);
        Assert.Equal(2, ageIndex.Count);

        var foundByName = await nameIndex.FindAsync("Alice");
        Assert.NotNull(foundByName);
        Assert.Equal(1, foundByName?.Id);

        var foundByAge = await ageIndex.FindAsync(30);
        Assert.NotNull(foundByAge);
        Assert.Equal("Bob", foundByAge?.Name);
    }

    [Fact]
    public async Task AjisIndex_EmptyFile_HandlesGracefully()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "empty.ajis");
        var context = new AjisContext();
        var users = context.Set<User>(filePath);

        await context.SaveChangesAsync(); // Creates empty file

        var index = new AjisIndex<User>(filePath, "Name");
        await index.BuildAsync();

        // Act & Assert
        Assert.Equal(0, index.Count);
        Assert.False(await index.ContainsAsync("Alice"));
    }

    private class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}
