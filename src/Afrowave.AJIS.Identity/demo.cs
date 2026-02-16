using Afrowave.AJIS.Identity;
using Microsoft.AspNetCore.Identity;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== AjisIdentity Demo ===\n");

        // Initialize data directory
        var basePath = Path.Combine(AppContext.BaseDirectory, "demo_data");
        Directory.CreateDirectory(basePath);

        // Create stores
        var userStore = new UserStore(basePath);
        var roleStore = new RoleStore(basePath);

        // Create default roles
        Console.WriteLine("Creating default roles...");
        await CreateDefaultRoles(roleStore);

        // Demo: Create a new user with profile
        Console.WriteLine("\n--- Demo: Creating User with Profile ---");
        var user = new User
        {
            UserName = "john.doe",
            Email = "john.doe@example.com",
            PhoneNumber = "+1-555-0123",
            Profile = new UserProfile
            {
                FirstName = "John",
                LastName = "Doe",
                Bio = "Software developer and AJIS enthusiast",
                ProfilePhotoPath = "https://via.placeholder.com/150",
                Location = "New York, NY",
                Website = "https://johndoe.com",
                CreatedAt = DateTime.UtcNow
            }
        };

        await userStore.CreateAsync(user, CancellationToken.None);
        await userStore.AddToRoleAsync(user, "User", CancellationToken.None);

        Console.WriteLine($"✓ User created: {user.UserName}");
        Console.WriteLine($"  - Email: {user.Email}");
        Console.WriteLine($"  - Profile: {user.Profile?.FirstName} {user.Profile?.LastName}");
        Console.WriteLine($"  - Roles: {string.Join(", ", user.RoleNames)}");

        // Demo: Find user by email
        Console.WriteLine("\n--- Demo: Finding User by Email ---");
        var foundUser = userStore.FindByEmail("JOHN.DOE@EXAMPLE.COM");
        if (foundUser != null)
        {
            Console.WriteLine($"✓ Found user: {foundUser.UserName}");
            Console.WriteLine($"  - Email: {foundUser.Email}");
            Console.WriteLine($"  - Profile Photo: {foundUser.Profile?.ProfilePhotoPath}");
        }

        // Demo: Update user profile
        Console.WriteLine("\n--- Demo: Updating User Profile ---");
        foundUser!.Profile!.Bio = "Senior software developer and AJIS contributor";
        foundUser.Profile.UpdatedAt = DateTime.UtcNow;
        await userStore.UpdateAsync(foundUser, CancellationToken.None);
        Console.WriteLine("✓ Profile updated!");

        // Demo: Password setup
        Console.WriteLine("\n--- Demo: Setting Password ---");
        var passwordHasher = new PasswordHasher<User>();
        var hashedPassword = passwordHasher.HashPassword(foundUser, "SecurePassword123!");
        await userStore.SetPasswordHashAsync(foundUser, hashedPassword, CancellationToken.None);
        await userStore.UpdateAsync(foundUser, CancellationToken.None);
        Console.WriteLine("✓ Password set (hashed)");

        // Demo: Promote to admin
        Console.WriteLine("\n--- Demo: Promoting to Admin ---");
        await userStore.AddToRoleAsync(foundUser, "Admin", CancellationToken.None);
        var roles = await userStore.GetRolesAsync(foundUser, CancellationToken.None);
        Console.WriteLine($"✓ Promoted to Admin. Current roles: {string.Join(", ", roles)}");

        // Demo: Create another user with profile photo
        Console.WriteLine("\n--- Demo: Creating User with Profile Photo ---");
        var adminUser = new User
        {
            UserName = "admin",
            Email = "admin@example.com",
            Profile = new UserProfile
            {
                FirstName = "System",
                LastName = "Admin",
                ProfilePhotoPath = "/images/profile/admin.png",
                CreatedAt = DateTime.UtcNow
            }
        };

        await userStore.CreateAsync(adminUser, CancellationToken.None);
        await userStore.AddToRoleAsync(adminUser, "Admin", CancellationToken.None);
        Console.WriteLine($"✓ Admin user created: {adminUser.UserName}");
        Console.WriteLine($"  - Profile Photo: {adminUser.Profile?.ProfilePhotoPath}");

        // Demo: Query all users
        Console.WriteLine("\n--- Demo: Listing All Users ---");
        var allUsers = userStore.LoadUsers();
        foreach (var u in allUsers)
        {
            Console.WriteLine($"  - {u.UserName}");
            Console.WriteLine($"    Email: {u.Email}");
            if (u.Profile != null)
            {
                Console.WriteLine($"    Name: {u.Profile.FirstName} {u.Profile.LastName}");
                Console.WriteLine($"    Photo: {u.Profile.ProfilePhotoPath}");
            }
        }

        // Demo: Role management
        Console.WriteLine("\n--- Demo: Role Management ---");
        var usersInRole = allUsers.Where(u => u.RoleNames.Contains("Admin")).ToList();
        Console.WriteLine($"Admins count: {usersInRole.Count}");
        foreach (var admin in usersInRole)
        {
            Console.WriteLine($"  - {admin.UserName}");
        }

        // Demo: User with role removal
        Console.WriteLine("\n--- Demo: Removing User from Admin Role ---");
        await userStore.RemoveFromRoleAsync(foundUser, "Admin", CancellationToken.None);
        var updatedRoles = await userStore.GetRolesAsync(foundUser, CancellationToken.None);
        Console.WriteLine($"✓ Removed Admin role. Current roles: {string.Join(", ", updatedRoles)}");

        Console.WriteLine("\n=== Demo Complete ===");
        Console.WriteLine($"\nData files created in: {basePath}");
        Console.WriteLine("  - users.json");
        Console.WriteLine("  - roles.json");
    }

    static async Task CreateDefaultRoles(RoleStore roleStore)
    {
        var userRole = roleStore.FindByName("USER");
        if (userRole == null)
        {
            await roleStore.CreateAsync(new Role { Name = "User" }, CancellationToken.None);
            Console.WriteLine("  - Role created: User");
        }

        var adminRole = roleStore.FindByName("ADMIN");
        if (adminRole == null)
        {
            await roleStore.CreateAsync(new Role { Name = "Admin" }, CancellationToken.None);
            Console.WriteLine("  - Role created: Admin");
        }
    }
}
