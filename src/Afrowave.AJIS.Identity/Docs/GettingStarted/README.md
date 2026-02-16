# Getting Started with AjisIdentity

This guide will help you get started with the AjisIdentity project.

## Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or VS Code (optional)
- Git

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/afrowaveltd/AjisIdentity.git
cd AjisIdentity
```

### 2. Build the Project

```bash
dotnet build
```

### 3. Run the Application

```bash
dotnet run --project src/AjisIdentity/src/AjisIdentity.csproj
```

The application will start and output:
```
Now listening on: https://localhost:5001
Now listening on: http://localhost:5000
```

### 4. Open in Browser

Navigate to:
- [https://localhost:5001](https://localhost:5001)

## First-Time Setup

On first run, the application will:

1. Create the `data` folder in the application directory
2. Initialize `users.json` - stores all user accounts
3. Initialize `roles.json` - stores role definitions
4. Create default roles (User, Admin)

## Basic Usage

### Register a New User

1. Navigate to `/Account/Register`
2. Fill in the registration form:
   - Email
   - Username
   - Password
   - First Name (optional)
   - Last Name (optional)
3. Click "Register"

### Login

1. Navigate to `/Account/Login`
2. Enter your email and password
3. Click "Log in"

### Manage Profile

1. Login to your account
2. Navigate to `/Account/Manage/Index`
3. Update your profile information

### Admin Functions

If you have Admin role:

1. Navigate to `/Admin/Users`
2. View all users
3. Manage user roles

## Project Structure

```
src/
├── AjisIdentity/           # Main web application
│   ├── Pages/
│   │   ├── Account/       # Authentication pages
│   │   └── Admin/         # Admin pages
│   ├── Program.cs         # Application entry point
│   └── Startup.cs         # Configuration
├── AjisIdentity.Domain/   # Domain models
│   └── Models.cs          # User, Role, UserProfile
└── AjisIdentity.Ajis/     # Persistence layer
    ├── UserStore.cs       # User persistence
    └── RoleStore.cs       # Role persistence
```

## Data Files

All data is stored in JSON files:

- `data/users.json` - User accounts
- `data/roles.json` - Role definitions
- `data/profiles.json` - User profiles (optional)

### Example User Record

```json
{
  "Id": "uuid",
  "UserName": "john_doe",
  "NormalizedUserName": "JOHN_DOE",
  "Email": "john@example.com",
  "NormalizedEmail": "JOHN@EXAMPLE.COM",
  "EmailConfirmed": true,
  "PasswordHash": "hashed_password",
  "RoleNames": ["User"],
  "Profile": {
    "UserId": "uuid",
    "FirstName": "John",
    "LastName": "Doe",
    "CreatedAt": "2024-01-01T00:00:00Z"
  }
}
```

## Key Components

### UserStore

The `UserStore` class implements ASP.NET Core Identity interfaces and uses AJIS for persistence:

```csharp
public class UserStore : 
    IUserStore<User>,
    IUserPasswordStore<User>,
    IUserRoleStore<User>,
    IUserEmailStore<User>,
    IUserLockoutStore<User>,
    IUserPhoneNumberStore<User>,
    IUserSecurityStampStore<User>
```

### RoleStore

The `RoleStore` class provides role management:

```csharp
public class RoleStore : 
    IRoleStore<Role>,
    IRoleLockoutStore<Role>
```

### AjisContext

The `AjisContext` provides file-based data access:

```csharp
var ajisContext = new AjisContext(basePath);
var users = ajisContext.Query<User>("users.json").ToList();
```

## Customization

### Change Data Storage Location

Edit `Startup.cs`:

```csharp
services.AddSingleton<AjisContext>(sp =>
{
    var basePath = Path.Combine(AppContext.BaseDirectory, "my-data");
    Directory.CreateDirectory(basePath);
    return new AjisContext(basePath);
});
```

### Add Custom User Properties

1. Edit `Models.cs` in AjisIdentity.Domain
2. Add your property to the `User` class
3. Update the UI forms as needed

### Change Default Roles

Edit `Register.cshtml.cs`:

```csharp
await _roleStore.CreateAsync(new Role { Name = "Viewer" }, CancellationToken.None);
await _roleStore.CreateAsync(new Role { Name = "Editor" }, CancellationToken.None);
```

## Troubleshooting

### Data Files Not Created

Ensure the application has write permissions to the `data` directory.

### Login Issues

1. Check that `users.json` exists in the `data` folder
2. Verify the email and password are correct
3. Check the browser console for errors

### Permissions Errors

Run the application with appropriate permissions or change the data directory location.

## Next Steps

- Read the [Architecture](../Architecture/README.md) documentation
- Explore the [tests](../../../tests/) for examples
- Customize the UI in the `Pages` folder
- Add more features to the domain models

## Support

For issues and questions:

- Check the [docs](../)
- Review the [tests](../../../tests/)
- Open an issue on GitHub
