using Microsoft.AspNetCore.Identity;

namespace Afrowave.AJIS.Identity;

/// <summary>
/// Custom User class implementing ASP.NET Core Identity interfaces.
/// </summary>
public class User : IdentityUser<string>
{
    public UserProfile? Profile { get; set; }

    public List<string> RoleNames { get; set; } = new();
}

/// <summary>
/// Custom Role class implementing ASP.NET Core Identity interfaces.
/// </summary>
public class Role : IdentityRole<string>
{
    public List<string> UserNames { get; set; } = new();
}

/// <summary>
/// Represents user profile information stored in AJIS.
/// </summary>
public class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhotoPath { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
