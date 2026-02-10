#nullable enable

using Afrowave.AJIS.IO;
using Afrowave.AJIS.Serialization.Mapping;
using System.Text.Json;
using Xunit;

namespace Afrowave.AJIS.IO.Tests;

public class AjisFileTests : IDisposable
{
    private readonly string _testFile = "test_users.json";

    public AjisFileTests()
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
    public void Create_ValidItems_CreatesFile()
    {
        var users = new[]
        {
            new TestUser { Id = 1, Name = "Alice", Email = "alice@test.com" },
            new TestUser { Id = 2, Name = "Bob", Email = "bob@test.com" }
        };

        AjisFile.Create(_testFile, users);

        Assert.True(File.Exists(_testFile));
        var content = File.ReadAllText(_testFile);
        Assert.Contains("Alice", content);
        Assert.Contains("Bob", content);
    }

    [Fact]
    public void ReadAll_ExistingFile_ReturnsItems()
    {
        var users = new[]
        {
            new TestUser { Id = 1, Name = "Alice", Email = "alice@test.com" }
        };

        AjisFile.Create(_testFile, users);
        var loadedUsers = AjisFile.ReadAll<TestUser>(_testFile);

        Assert.Single(loadedUsers);
        Assert.Equal("Alice", loadedUsers[0].Name);
        Assert.Equal("alice@test.com", loadedUsers[0].Email);
    }

    [Fact]
    public void Append_ExistingFile_AppendsItem()
    {
        var initialUsers = new[] { new TestUser { Id = 1, Name = "Alice" } };
        AjisFile.Create(_testFile, initialUsers);

        var newUser = new TestUser { Id = 2, Name = "Bob" };
        AjisFile.Append(_testFile, newUser);

        var allUsers = AjisFile.ReadAll<TestUser>(_testFile);
        Assert.Equal(2, allUsers.Count);
        Assert.Equal("Bob", allUsers[1].Name);
    }

    [Fact]
    public void FindByKey_WithIndex_ReturnsCorrectItem()
    {
        var users = new[]
        {
            new TestUser { Id = 1, Name = "Alice" },
            new TestUser { Id = 2, Name = "Bob" }
        };

        AjisFile.Create(_testFile, users);
        var foundUser = AjisFile.FindByKey<TestUser>(_testFile, "Name", "Bob");

        Assert.NotNull(foundUser);
        Assert.Equal("Bob", foundUser.Name);
    }

    [Fact]
    public void Get_SimpleApi_ReturnsCorrectItem()
    {
        var users = new[] { new TestUser { Id = 1, Name = "Alice" } };
        AjisFile.Create(_testFile, users);

        var user = AjisFile.Get<TestUser>(_testFile, "Name", "Alice");

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public void UpdateByKey_ExistingItem_UpdatesItem()
    {
        var users = new[] { new TestUser { Id = 1, Name = "Alice", Email = "old@test.com" } };
        AjisFile.Create(_testFile, users);

        AjisFile.UpdateByKey<TestUser>(_testFile, "Name", "Alice", user =>
        {
            user.Email = "new@test.com";
        });

        var updatedUsers = AjisFile.ReadAll<TestUser>(_testFile);
        Assert.Equal("new@test.com", updatedUsers[0].Email);
    }

    [Fact]
    public void DeleteByKey_ExistingItem_RemovesItem()
    {
        var users = new[]
        {
            new TestUser { Id = 1, Name = "Alice" },
            new TestUser { Id = 2, Name = "Bob" }
        };

        AjisFile.Create(_testFile, users);
        AjisFile.DeleteByKey<TestUser>(_testFile, "Name", "Alice");

        var remainingUsers = AjisFile.ReadAll<TestUser>(_testFile);
        Assert.Single(remainingUsers);
        Assert.Equal("Bob", remainingUsers[0].Name);
    }

    [Fact]
    public void Enumerate_LargeFile_StreamsItems()
    {
        var users = Enumerable.Range(1, 1000)
            .Select(i => new TestUser { Id = i, Name = $"User{i}" })
            .ToArray();

        AjisFile.Create(_testFile, users);
        var enumeratedUsers = AjisFile.Enumerate<TestUser>(_testFile).ToList();

        Assert.Equal(1000, enumeratedUsers.Count);
        Assert.Equal("User1", enumeratedUsers[0].Name);
        Assert.Equal("User1000", enumeratedUsers[999].Name);
    }

    private class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }
}