#nullable enable

using Afrowave.AJIS.IO;
using Afrowave.AJIS.Core;
using Xunit;

namespace Afrowave.AJIS.IO.Tests;

public class AjisContextTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "AjisContextTests");

    public AjisContextTests()
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
    public async Task AjisContext_Sets_AreReused()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.ajis");
        var context = new AjisContext();

        // Act
        var set1 = context.Set<TestItem>(filePath);
        var set2 = context.Set<TestItem>(filePath);

        // Assert
        Assert.Same(set1, set2);
    }

    [Fact]
    public async Task AjisContext_AddAndSave_CreateFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "addtest.ajis");
        var context = new AjisContext();
        var set = context.Set<TestItem>(filePath);

        // Act
        await set.AddAsync(new TestItem { Id = 1, Name = "Test1" });
        await context.SaveChangesAsync();

        // Assert
        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.Contains("Test1", content);
    }

    [Fact]
    public async Task AjisContext_AddMultipleAndSave()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "addmultiple.ajis");
        var context = new AjisContext();
        var set = context.Set<TestItem>(filePath);

        // Act
        await set.AddAsync(new TestItem { Id = 1, Name = "Test1" });
        await set.AddAsync(new TestItem { Id = 2, Name = "Test2" });
        await set.AddAsync(new TestItem { Id = 3, Name = "Test3" });
        await context.SaveChangesAsync();

        // Assert
        var count = await set.CountAsync();
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task AjisContext_FindByKey_FindsItem()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "findtest.ajis");
        var context = new AjisContext();
        var set = context.Set<TestItem>(filePath);

        // Act
        await set.AddAsync(new TestItem { Id = 1, Name = "Test1" });
        await set.AddAsync(new TestItem { Id = 2, Name = "Test2" });
        await context.SaveChangesAsync();

        var found = await set.FindByKeyAsync(2);
        
        // Assert
        Assert.NotNull(found);
        Assert.Equal("Test2", found?.Name);
    }

    [Fact]
    public async Task AjisContext_UpdateEntity_UpdatesFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "updatetest.ajis");
        var context = new AjisContext();
        var set = context.Set<TestItem>(filePath);

        // Act
        await set.AddAsync(new TestItem { Id = 1, Name = "Original" });
        await context.SaveChangesAsync();

        var item = await set.FindByKeyAsync(1);
        item!.Name = "Updated";
        await set.UpdateAsync(item);
        await context.SaveChangesAsync();

        // Assert
        var itemAfter = await set.FindByKeyAsync(1);
        Assert.Equal("Updated", itemAfter?.Name);
    }

    [Fact]
    public async Task AjisContext_RemoveEntity_RemovesFromFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "removetest.ajis");
        var context = new AjisContext();
        var set = context.Set<TestItem>(filePath);

        // Act
        await set.AddAsync(new TestItem { Id = 1, Name = "ToRemove" });
        await set.AddAsync(new TestItem { Id = 2, Name = "ToKeep" });
        await context.SaveChangesAsync();

        var itemToRemove = await set.FindByKeyAsync(1);
        await set.RemoveAsync(itemToRemove!);
        await context.SaveChangesAsync();

        // Assert
        var count = await set.CountAsync();
        Assert.Equal(1, count);
        var remaining = await set.FindByKeyAsync(2);
        Assert.NotNull(remaining);
    }

    [Fact]
    public async Task AjisContext_CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "counttest.ajis");
        var context = new AjisContext();
        var set = context.Set<TestItem>(filePath);

        // Act
        for (int i = 1; i <= 10; i++)
        {
            await set.AddAsync(new TestItem { Id = i, Name = $"Item{i}" });
        }
        await context.SaveChangesAsync();

        // Assert
        var count = await set.CountAsync();
        Assert.Equal(10, count);
    }

    [Fact]
    public async Task AjisContext_AnyAsync_WithPredicate()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "anytest.ajis");
        var context = new AjisContext();
        var set = context.Set<TestItem>(filePath);

        // Act
        await set.AddAsync(new TestItem { Id = 1, Name = "Item1" });
        await set.AddAsync(new TestItem { Id = 2, Name = "Item2" });
        await context.SaveChangesAsync();

        // Assert
        var exists = await set.AnyAsync(i => i.Name == "Item2");
        Assert.True(exists);

        var notExists = await set.AnyAsync(i => i.Name == "Item3");
        Assert.False(notExists);
    }

    [Fact]
    public async Task AjisContext_CountAsync_WithPredicate()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "predicatetest.ajis");
        var context = new AjisContext();
        var set = context.Set<TestItem>(filePath);

        // Act
        await set.AddAsync(new TestItem { Id = 1, Name = "Adult", Age = 25 });
        await set.AddAsync(new TestItem { Id = 2, Name = "Child", Age = 10 });
        await set.AddAsync(new TestItem { Id = 3, Name = "Adult2", Age = 30 });
        await context.SaveChangesAsync();

        // Assert
        var adultCount = await set.CountAsync(i => i.Age >= 18);
        Assert.Equal(2, adultCount);
    }

    [Fact]
    public async Task AjisContext_SetWithFormat_Ajis()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.ajis");
        var context = new AjisContext();

        // Act
        var set = context.Set<TestItem>(filePath, AjisFormat.Ajis);

        // Assert
        Assert.Equal(AjisFormat.Ajis, set.Format);
    }

    [Fact]
    public async Task AjisContext_SetWithFormat_Atp()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.atp");
        var context = new AjisContext();

        // Act
        var set = context.Set<TestItem>(filePath, AjisFormat.Atp);

        // Assert
        Assert.Equal(AjisFormat.Atp, set.Format);
    }

    [Fact]
    public async Task AjisContext_AutoDetectFormat_ByExtension_Ajis()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.json");
        var context = new AjisContext();

        // Act
        var set = context.Set<TestItem>(filePath);

        // Assert
        Assert.Equal(AjisFormat.Ajis, set.Format);
    }

    [Fact]
    public async Task AjisContext_AutoDetectFormat_ByExtension_Atp()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.atp");
        var context = new AjisContext();

        // Act
        var set = context.Set<TestItem>(filePath);

        // Assert
        Assert.Equal(AjisFormat.Atp, set.Format);
    }

    [Fact]
    public async Task AjisContext_AutoDetectFormat_ByConfiguration_HasBinaryAttachments()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.ajis");
        var context = new AjisContext();
        var config = new AjisEntityConfiguration<TestItemWithAttachment>();
        config.BinaryAttachment(i => i.Attachment);

        // Act
        var set = context.Set<TestItemWithAttachment>(filePath, AjisFormat.Auto, config);

        // Assert
        Assert.Equal(AjisFormat.Atp, set.Format);
    }

    [Fact]
    public async Task AjisContext_MultipleSets_OperateIndependently()
    {
        // Arrange
        var filePath1 = Path.Combine(_testDirectory, "set1.ajis");
        var filePath2 = Path.Combine(_testDirectory, "set2.ajis");
        var context = new AjisContext();
        var set1 = context.Set<TestItem>(filePath1);
        var set2 = context.Set<TestItem>(filePath2);

        // Act
        await set1.AddAsync(new TestItem { Id = 1, Name = "Set1Item" });
        await set2.AddAsync(new TestItem { Id = 2, Name = "Set2Item" });
        await context.SaveChangesAsync();

        // Assert
        // Each set reads from its own file, so counts should be 1
        Assert.Equal(1, await set1.CountAsync());
        Assert.Equal(1, await set2.CountAsync());
        // Items shouldn't be visible across files
        Assert.Null(await set1.FindByKeyAsync(2));
        Assert.Null(await set2.FindByKeyAsync(1));
    }

    [Fact]
    public async Task AjisContext_ExplicitKeyPropertyName_Works()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "keytest.ajis");
        var context = new AjisContext();
        var set = context.Set<TestItem>(filePath, "GuidId");

        // Act
        await set.AddAsync(new TestItem { GuidId = Guid.NewGuid(), Name = "Test1" });
        await context.SaveChangesAsync();

        await set.AddAsync(new TestItem { GuidId = Guid.NewGuid(), Name = "Test2" });
        await context.SaveChangesAsync();

        // Assert - key property name is set
        Assert.Equal("GuidId", set.KeyPropertyName);
    }

    [Fact]
    public async Task AjisContext_GetSet_ReturnsExistingSet()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "getsettest.ajis");
        var context = new AjisContext();
        context.Set<TestItem>(filePath);

        // Act
        var retrievedSet = context.GetSet<TestItem>(filePath);

        // Assert
        Assert.NotNull(retrievedSet);
        Assert.Equal(filePath, retrievedSet!.FilePath);
    }

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public Guid GuidId { get; set; } = Guid.NewGuid();
    }

    private class TestItemWithAttachment
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public BinaryAttachment Attachment { get; set; } = new();
    }
}
