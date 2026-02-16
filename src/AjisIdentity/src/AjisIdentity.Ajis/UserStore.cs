using Microsoft.AspNetCore.Identity;

namespace Afrowave.AJIS.Identity;

/// <summary>
/// AJIS-based UserStore implementation for ASP.NET Core Identity.
/// </summary>
public class UserStore : IUserStore<User>,
    IUserPasswordStore<User>,
    IUserRoleStore<User>,
    IUserEmailStore<User>,
    IUserLockoutStore<User>,
    IUserPhoneNumberStore<User>,
    IUserSecurityStampStore<User>,
    IDisposable
{
    private readonly string _basePath;
    private bool _disposed;

    public string BasePath => _basePath;

    public UserStore(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(basePath);
    }

    private string UsersPath => Path.Combine(_basePath, "users.json");

    private List<User> LoadUsers()
    {
        if (File.Exists(UsersPath))
        {
            var json = File.ReadAllText(UsersPath);
            return System.Text.Json.JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
        }
        return new List<User>();
    }

    private void SaveUsers(List<User> users)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(users, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(UsersPath, json);
    }

    private List<User> LoadAllUsers()
    {
        return LoadUsers();
    }

    #region IUserStore<User> implementation
    public Task<string?> GetUserIdAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedUserName);
    }

    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        user.Id = user.Id ?? Guid.NewGuid().ToString();
        user.SecurityStamp = user.SecurityStamp ?? Guid.NewGuid().ToString();
        user.ConcurrencyStamp = user.ConcurrencyStamp ?? Guid.NewGuid().ToString();

        var users = LoadUsers();
        users.Add(user);
        SaveUsers(users);

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        user.ConcurrencyStamp = Guid.NewGuid().ToString();

        var users = LoadUsers();
        var existingUser = users.FirstOrDefault(u => u.Id == user.Id);
        if (existingUser != null)
        {
            var index = users.IndexOf(existingUser);
            users[index] = user;
        }
        else
        {
            users.Add(user);
        }
        SaveUsers(users);

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var users = LoadUsers();
        users.RemoveAll(u => u.Id == user.Id);
        SaveUsers(users);

        return IdentityResult.Success;
    }

    public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        var users = LoadUsers();
        var user = users.FirstOrDefault(u => u.Id == userId);
        return Task.FromResult(user);
    }

    public Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        var users = LoadUsers();
        var user = users.FirstOrDefault(u => u.NormalizedUserName == normalizedUserName);
        return Task.FromResult(user);
    }

    #endregion

    #region IUserPasswordStore<User> implementation
    public async Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        await UpdateAsync(user, cancellationToken);
    }

    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
    }
    #endregion

    #region IUserRoleStore<User> implementation
    public async Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        if (!user.RoleNames.Contains(roleName))
        {
            user.RoleNames.Add(roleName);
            await UpdateAsync(user, cancellationToken);
        }
    }

    public async Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        if (user.RoleNames.Contains(roleName))
        {
            user.RoleNames.Remove(roleName);
            await UpdateAsync(user, cancellationToken);
        }
    }

    public Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<IList<string>>(user.RoleNames.ToList());
    }

    public Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.RoleNames.Contains(roleName));
    }

    public Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var users = LoadUsers();
        var usersInRole = users.Where(u => u.RoleNames.Contains(roleName)).ToList();
        return Task.FromResult<IList<User>>(usersInRole);
    }
    #endregion

    #region IUserEmailStore<User> implementation
    public async Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        user.NormalizedEmail = email?.ToUpperInvariant();
        await UpdateAsync(user, cancellationToken);
    }

    public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.EmailConfirmed);
    }

    public async Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        await UpdateAsync(user, cancellationToken);
    }

    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var users = LoadUsers();
        var user = users.FirstOrDefault(u => u.NormalizedEmail == normalizedEmail);
        return Task.FromResult(user);
    }

    public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetNormalizedEmailAsync(User user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedName;
        return Task.CompletedTask;
    }
    #endregion

    #region IUserLockoutStore<User> implementation
    public async Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        await UpdateAsync(user, cancellationToken);
    }

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnd);
    }

    public Task<int> GetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task<bool> GetLockoutEnabledAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnabled);
    }

    public Task SetLockoutEnabledAsync(User user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    public async Task<int> IncrementAccessFailedCountAsync(User user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        await UpdateAsync(user, cancellationToken);
        return user.AccessFailedCount;
    }

    public async Task ResetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        await UpdateAsync(user, cancellationToken);
    }
    #endregion

    #region Public methods for admin pages

    public List<User> GetAllUsers()
    {
        return LoadUsers();
    }

    #endregion

    #region IUserPhoneNumberStore<User> implementation
    public async Task SetPhoneNumberAsync(User user, string? phoneNumber, CancellationToken cancellationToken)
    {
        user.PhoneNumber = phoneNumber;
        await UpdateAsync(user, cancellationToken);
    }

    public Task<string?> GetPhoneNumberAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumber);
    }

    public Task<bool> GetPhoneNumberConfirmedAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumberConfirmed);
    }

    public async Task SetPhoneNumberConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        user.PhoneNumberConfirmed = confirmed;
        await UpdateAsync(user, cancellationToken);
    }
    #endregion

    #region IUserSecurityStampStore<User> implementation
    public async Task SetSecurityStampAsync(User user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        await UpdateAsync(user, cancellationToken);
    }

    public Task<string?> GetSecurityStampAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.SecurityStamp);
    }
    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
