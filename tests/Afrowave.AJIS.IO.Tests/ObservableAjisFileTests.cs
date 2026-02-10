#nullable enable

using Afrowave.AJIS.IO;
using Xunit;

namespace Afrowave.AJIS.IO.Tests;

public class ObservableAjisFileTests : IDisposable
{
    private readonly string _testFile = "test_observable_users.json";

    public ObservableAjisFileTests()
    {
        if (File.Exists(_testFile))
            File.Delete(_testFile);
    }

    public void Dispose()
    {
        if (File.Exists(_testFile))
            File.Delete(_testFile);
    }

    [Fact]
    public async Task Subscribe_Add_TriggersEvent()
    {
        var observableFile = new ObservableAjisFile<TestUser>(_testFile);

        var events = new List<(TestUser User, ObservableAjisFile<TestUser>.ChangeType ChangeType)>();
        observableFile.Subscribe((user, changeType) => events.Add((user, changeType)));

        var user = new TestUser { Id = 1, Name = "Alice" };
        observableFile.Add(user);

        await Task.Delay(100); // Allow event processing

        Assert.Single(events);
        Assert.Equal("Alice", events[0].User.Name);
        Assert.Equal(ObservableAjisFile<TestUser>.ChangeType.Added, events[0].ChangeType);
    }

    [Fact]
    public async Task Subscribe_Update_TriggersEvent()
    {
        var observableFile = new ObservableAjisFile<TestUser>(_testFile);

        // Add initial user
        observableFile.Add(new TestUser { Id = 1, Name = "Alice", Email = "old@test.com" });
        await Task.Delay(1500); // Wait for save

        var events = new List<(TestUser User, ObservableAjisFile<TestUser>.ChangeType ChangeType)>();
        observableFile.Subscribe((user, changeType) => events.Add((user, changeType)));

        // Update user
        observableFile.Update(new TestUser { Id = 1, Name = "Alice", Email = "new@test.com" },
                             u => u.Name == "Alice");

        await Task.Delay(100);

        Assert.Single(events);
        Assert.Equal("new@test.com", events[0].User.Email);
        Assert.Equal(ObservableAjisFile<TestUser>.ChangeType.Updated, events[0].ChangeType);
    }

    [Fact]
    public async Task Subscribe_Delete_TriggersEvent()
    {
        var observableFile = new ObservableAjisFile<TestUser>(_testFile);

        // Add user
        var user = new TestUser { Id = 1, Name = "Alice" };
        observableFile.Add(user);
        await Task.Delay(1500);

        var events = new List<(TestUser User, ObservableAjisFile<TestUser>.ChangeType ChangeType)>();
        observableFile.Subscribe((user, changeType) => events.Add((user, changeType)));

        // Delete user
        observableFile.Delete(u => u.Name == "Alice");

        await Task.Delay(100);

        Assert.Single(events);
        Assert.Equal("Alice", events[0].User.Name);
        Assert.Equal(ObservableAjisFile<TestUser>.ChangeType.Deleted, events[0].ChangeType);
    }

    [Fact]
    public async Task MultipleSubscribers_AllReceiveEvents()
    {
        var observableFile = new ObservableAjisFile<TestUser>(_testFile);

        var events1 = new List<string>();
        var events2 = new List<string>();

        observableFile.Subscribe((user, changeType) => events1.Add($"{changeType}:{user.Name}"));
        observableFile.Subscribe((user, changeType) => events2.Add($"{changeType}:{user.Name}"));

        observableFile.Add(new TestUser { Id = 1, Name = "Alice" });

        await Task.Delay(100);

        Assert.Single(events1);
        Assert.Single(events2);
        Assert.Equal("Added:Alice", events1[0]);
        Assert.Equal("Added:Alice", events2[0]);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCurrentData()
    {
        var observableFile = new ObservableAjisFile<TestUser>(_testFile);

        observableFile.Add(new TestUser { Id = 1, Name = "Alice" });
        observableFile.Add(new TestUser { Id = 2, Name = "Bob" });
        await Task.Delay(1500);

        var users = await observableFile.GetAllAsync();
        Assert.Equal(2, users.Count);
        Assert.Equal("Alice", users[0].Name);
        Assert.Equal("Bob", users[1].Name);
    }

    private class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }
}