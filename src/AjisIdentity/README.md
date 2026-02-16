# AjisIdentity - ASP.NET Core Identity with AJIS

A demonstration project showing how to integrate ASP.NET Core Identity with AJIS (Another JSON-based Information System) for user data and profile management.

## Project Structure

```
AjisIdentity/
├── src/
│   └── AjisIdentity/              # Main ASP.NET Core Web App
│   │   ├── Pages/
│   │   │   ├── Account/
│   │   │   │   ├── Login.cshtml
│   │   │   │   ├── Register.cshtml
│   │   │   │   └── Index.cshtml
│   │   │   ├── Admin/
│   │   │   │   ├── Users.cshtml
│   │   │   │   └── Roles.cshtml
│   │   │   ├── Index.cshtml
│   │   │   ├── Program.cs
│   │   │   └── Startup.cs
│   │   └── AjisIdentity.csproj
│   │
│   └── AjisIdentity.Domain/       # Domain Models
│   │   ├── Models.cs              # User, Role, UserProfile
│   │   └── AjisIdentity.Domain.csproj
│   │
│   └── AjisIdentity.Ajis/         # AJIS Persistence Layer
│   │   ├── UserStore.cs           # Custom UserStore implementation
│   │   ├── RoleStore.cs           # Custom RoleStore implementation
│   │   └── AjisIdentity.Ajis.csproj
│   │
│   └── tests/
│       └── AjisIdentity.Tests/    # Unit Tests
│           ├── UserTests.cs
│           ├── UserStoreTests.cs
│           ├── RoleStoreTests.cs
│           └── AjisIdentity.Tests.csproj
│
├── Docs/
│   ├── GettingStarted/            # Getting Started Guide
│   │   └── README.md
│   └── Architecture/              # Architecture Documentation
│       └── README.md
│
└── README.md                       # This file
```

## Features

- ✅ User registration and login with ASP.NET Core Identity
- ✅ Role-based authorization (Admin, User, custom roles)
- ✅ User profiles with photos (stored in JSON files)
- ✅ All data managed through file I/O (no database required)
- ✅ File-based persistence (users.json, roles.json, profiles.json)
- ✅ Email confirmation support
- ✅ Lockout support
- ✅ Security stamp support
- ✅ Phone number support

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code

### Build and Run

```bash
cd src/AjisIdentity
dotnet build
dotnet run
```

The application will be available at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

## Data Storage

All user data is stored in JSON files within the `data` directory:

- **users.json** - User accounts and credentials
- **roles.json** - Role definitions
- **profiles.json** - User profile information (optional)

Example `data/users.json`:
```json
[
  {
    "Id": "123e4567-e89b-12d3-a456-426614174000",
    "UserName": "admin",
    "NormalizedUserName": "ADMIN",
    "Email": "admin@example.com",
    "NormalizedEmail": "ADMIN@EXAMPLE.COM",
    "EmailConfirmed": true,
    "PasswordHash": "AQAAAAEAACcQAAAAE...",
    "SecurityStamp": "abc123...",
    "ConcurrencyStamp": "def456...",
    "RoleNames": ["Admin"],
    "Profile": {
      "UserId": "123e4567-e89b-12d3-a456-426614174000",
      "FirstName": "Admin",
      "LastName": "User",
      "CreatedAt": "2024-01-01T00:00:00Z"
    }
  }
]
```

## Key Components

### UserStore

The `UserStore` class implements all ASP.NET Core Identity interfaces:

- `IUserStore<User>`
- `IUserPasswordStore<User>`
- `IUserRoleStore<User>`
- `IUserEmailStore<User>`
- `IUserLockoutStore<User>`
- `IUserPhoneNumberStore<User>`
- `IUserSecurityStampStore<User>`

### RoleStore

The `RoleStore` class provides role management:

- `IRoleStore<Role>`
- `IRoleLockoutStore<Role>`

### Data Storage

Uses simple JSON file I/O for persistence:

```csharp
// Load users from file
var users = LoadUsers();

// Save users to file
SaveUsers(users);
```

## Customization

### Change Storage Location

Modify `Startup.cs` or `Program.cs`:

```csharp
var basePath = Path.Combine(AppContext.BaseDirectory, "custom-data");
var userStore = new UserStore(basePath);
var roleStore = new RoleStore(basePath);
```

### Add Custom User Properties

1. Edit `Models.cs` in `AjisIdentity.Domain`
2. Add your property to the `User` class
3. Update the UI forms as needed

### Change Default Roles

Edit registration logic in `Pages/Account/Register.cshtml.cs`:

```csharp
await roleStore.CreateAsync(new Role { Name = "Viewer" }, CancellationToken.None);
await roleStore.CreateAsync(new Role { Name = "Editor" }, CancellationToken.None);
```

## Testing

Run the demo application:

```bash
dotnet run --project demo.cs
```

Or run tests:

```bash
dotnet test src/AjisIdentity/tests/AjisIdentity.Tests/AjisIdentity.Tests.csproj
```

## Documentation

- [Getting Started](Docs/GettingStarted/README.md) - Step-by-step setup guide
- [Architecture](Docs/Architecture/README.md) - Architecture details

## Next Steps

1. Read the [Getting Started](Docs/GettingStarted/README.md) guide
2. Explore the [tests](tests/) for examples
3. Customize the UI in the `Pages` folder
4. Add more features to the domain models

## License

This project is licensed under the MIT License.

## Author

Afrowave - [https://github.com/afrowaveltd](https://github.com/afrowaveltd)
