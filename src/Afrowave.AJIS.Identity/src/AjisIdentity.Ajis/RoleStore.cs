using Microsoft.AspNetCore.Identity;

namespace Afrowave.AJIS.Identity;

/// <summary>
/// AJIS-based RoleStore implementation for ASP.NET Core Identity.
/// </summary>
public class RoleStore : IRoleStore<Role>,
    IDisposable
{
    private readonly string _basePath;
    private bool _disposed;

    public string BasePath => _basePath;

    public RoleStore(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(basePath);
    }

    private string RolesPath => Path.Combine(_basePath, "roles.json");

    private List<Role> LoadRoles()
    {
        if (File.Exists(RolesPath))
        {
            var json = File.ReadAllText(RolesPath);
            return System.Text.Json.JsonSerializer.Deserialize<List<Role>>(json) ?? new List<Role>();
        }
        return new List<Role>();
    }

    private void SaveRoles(List<Role> roles)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(roles, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(RolesPath, json);
    }

    public async Task<IdentityResult> CreateAsync(Role role, CancellationToken cancellationToken)
    {
        if (role == null) throw new ArgumentNullException(nameof(role));

        role.Id = role.Id ?? Guid.NewGuid().ToString();
        role.NormalizedName = role.Name?.ToUpperInvariant();
        role.ConcurrencyStamp = role.ConcurrencyStamp ?? Guid.NewGuid().ToString();

        var roles = LoadRoles();
        roles.Add(role);
        SaveRoles(roles);

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(Role role, CancellationToken cancellationToken)
    {
        if (role == null) throw new ArgumentNullException(nameof(role));

        role.NormalizedName = role.Name?.ToUpperInvariant();
        role.ConcurrencyStamp = Guid.NewGuid().ToString();

        var roles = LoadRoles();
        var existingRole = roles.FirstOrDefault(r => r.Id == role.Id);
        if (existingRole != null)
        {
            var index = roles.IndexOf(existingRole);
            roles[index] = role;
        }
        else
        {
            roles.Add(role);
        }
        SaveRoles(roles);

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(Role role, CancellationToken cancellationToken)
    {
        if (role == null) throw new ArgumentNullException(nameof(role));

        var roles = LoadRoles();
        roles.RemoveAll(r => r.Id == role.Id);
        SaveRoles(roles);

        return IdentityResult.Success;
    }

    public Task<Role?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        var roles = LoadRoles();
        var role = roles.FirstOrDefault(r => r.Id == roleId);
        return Task.FromResult(role);
    }

    public Task<Role?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        var roles = LoadRoles();
        var role = roles.FirstOrDefault(r => r.NormalizedName == normalizedRoleName);
        return Task.FromResult(role);
    }

    public Task<string?> GetRoleIdAsync(Role role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.Id);
    }

    public Task<string?> GetRoleNameAsync(Role role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.Name);
    }

    public Task SetRoleNameAsync(Role role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedRoleNameAsync(Role role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.NormalizedName);
    }

    public Task SetNormalizedRoleNameAsync(Role role, string? normalizedRoleName, CancellationToken cancellationToken)
    {
        role.NormalizedName = normalizedRoleName;
        return Task.CompletedTask;
    }

    public Task SetLockoutEndDateAsync(Role role, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(Role role, CancellationToken cancellationToken)
    {
        return Task.FromResult((DateTimeOffset?)null);
    }

    public List<Role> GetAllRoles()
    {
        return LoadRoles();
    }

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
