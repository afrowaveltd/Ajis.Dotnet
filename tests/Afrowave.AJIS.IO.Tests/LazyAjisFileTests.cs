#nullable enable

using Afrowave.AJIS.IO;
using Afrowave.AJIS.Serialization.Mapping;
using System.Text.Json;
using Xunit;

namespace Afrowave.AJIS.IO.Tests;

public class LazyAjisFileTests : IDisposable
{
    private readonly string _testFile = "test_lazy_users.json";

    public LazyAjisFileTests()
    {
        // Clean up
        if (File.Exists(_testFile))
            File.Delete(_testFile);
    }

    public void Dispose()
    {
        // Clean up
        if (File.Exists(_testFile))
            File.Delete(_testFile);
    }

    [Fact]
    public async Task GetAllAsync_NewFile_ReturnsEmptyList()
    {
        using var lazyFile = new LazyAjisFile<TestUser>(_testFile);
        var users = await lazyFile.GetAllAsync();

        Assert.NotNull(users);
        Assert.Empty(users);
    }

    [Fact]
    public async Task Add_Item_AddsToFile()
    {
        using var lazyFile = new LazyAjisFile<TestUser>(_testFile);

        var user = new TestUser { Id = 1, Name = "Alice" };
        lazyFile.Add(user);

        // Wait for background save
        await Task.Delay(1500);

        var users = await lazyFile.GetAllAsync();
        Assert.Single(users);
        Assert.Equal("Alice", users[0].Name);
    }

    [Fact]
    public async Task GetAsync_ExistingItem_ReturnsItem()
    {
        using var lazyFile = new LazyAjisFile<TestUser>(_testFile);

        var user = new TestUser { Id = 1, Name = "Alice" };
        lazyFile.Add(user);
        await Task.Delay(1500); // Wait for save

        var foundUser = await lazyFile.GetAsync(u => u.Name == "Alice");
        Assert.NotNull(foundUser);
        Assert.Equal("Alice", foundUser.Name);
    }

    [Fact]
    public async Task Update_Item_UpdatesInFile()
    {
        using var lazyFile = new LazyAjisFile<TestUser>(_testFile);

        // Add initial user
        lazyFile.Add(new TestUser { Id = 1, Name = "Alice", Email = "old@test.com" });
        await Task.Delay(1500);

        lazyFile.Update(new TestUser { Id = 1, Name = "Alice", Email = "new@test.com" },
                       u => u.Name == "Alice");
        await Task.Delay(1500);

        var users = await lazyFile.GetAllAsync();
        Assert.Single(users);
        Assert.Equal("new@test.com", users[0].Email);
    }

    [Fact]
    public async Task Delete_Item_RemovesFromFile()
    {
        using var lazyFile = new LazyAjisFile<TestUser>(_testFile);

        lazyFile.Add(new TestUser { Id = 1, Name = "Alice" });
        lazyFile.Add(new TestUser { Id = 2, Name = "Bob" });
        await Task.Delay(1500);

        lazyFile.Delete(u => u.Name == "Alice");
        await Task.Delay(1500);

        var users = await lazyFile.GetAllAsync();
        Assert.Single(users);
        Assert.Equal("Bob", users[0].Name);
    }

    [Fact]
    public async Task FlushAsync_ForcesImmediateSave()
    {
        using var lazyFile = new LazyAjisFile<TestUser>(_testFile);

        lazyFile.Add(new TestUser { Id = 1, Name = "Alice" });
        await lazyFile.FlushAsync(); // Immediate save

        // Verify file exists and has content
        Assert.True(File.Exists(_testFile));
        var content = File.ReadAllText(_testFile);
        Assert.Contains("Alice", content);
    }

    [Fact]
    public async Task GetCountAsync_ReturnsCorrectCount()
    {
        using var lazyFile = new LazyAjisFile<TestUser>(_testFile);

        lazyFile.Add(new TestUser { Id = 1, Name = "Alice" });
        lazyFile.Add(new TestUser { Id = 2, Name = "Bob" });
        await Task.Delay(1500);

        var count = await lazyFile.GetCountAsync();
        Assert.Equal(2, count);
    }

    private class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }
}