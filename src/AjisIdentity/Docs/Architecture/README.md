# Architecture

This document describes the architecture of the AjisIdentity project.

## Overview

AjisIdentity is an ASP.NET Core web application that uses AJIS (Another JSON-based Information System) for data persistence instead of a traditional database. It provides full ASP.NET Core Identity functionality with user management, role management, and profile photos stored in JSON files.

## Architecture Layers

### 1. Presentation Layer (AjisIdentity)

**Location**: `src/AjisIdentity/`

Contains:
- Razor Pages for web interface
- Authentication pages (Login, Register)
- Admin management pages
- Identity UI integration

Components:
```
Pages/
├── Account/
│   ├── Login.cshtml
│   ├── Register.cshtml
│   └── Index.cshtml
├── Admin/
│   ├── Users.cshtml
│   └── Roles.cshtml
├── Index.cshtml
├── Program.cs
└── Startup.cs
```

**Responsibilities**:
- Handle HTTP requests
- Render Razor Pages
- Validate user input
- Display data to users

### 2. Domain Layer (AjisIdentity.Domain)

**Location**: `src/AjisIdentity.Domain/`

Contains:
- User model
- Role model
- UserProfile model

**Responsibilities**:
- Define core domain entities
- Implement ASP.NET Core Identity interfaces
- Define data contracts

### 3. Persistence Layer (AjisIdentity.Ajis)

**Location**: `src/AjisIdentity.Ajis/`

Contains:
- UserStore
- RoleStore
- Custom implementations for AJIS

**Responsibilities**:
- Implement ASP.NET Core Identity stores
- Use AJIS IO for file operations
- Handle data serialization/deserialization

## Data Flow

### User Registration Flow

1. User submits registration form (Razor Page)
2. Controller validates input
3. User object is created
4. UserStore.CreateAsync() is called
5. User is serialized to JSON
6. JSON is saved to `data/users.json`
7. Success response is returned

### User Login Flow

1. User submits login form
2. UserStore.FindByEmailAsync() queries users.json
3. Password is verified
4. Claims identity is created
5. User is signed in
6. Redirect to dashboard

### Role Assignment Flow

1. Admin assigns role via admin panel
2. RoleStore.AddToRoleAsync() is called
3. User's role list is updated
4. User is saved to users.json
5. Role list is updated in memory

## AJIS Integration

### Key Components

#### AjisContext

The central context for all AJIS operations:

```csharp
var ajisContext = new AjisContext(basePath);
var users = ajisContext.Query<User>("users.json").ToList();
ajisContext.Save(users, "users.json");
```

**Methods**:
- `Query<T>()` - Query data from file
- `Save<T>()` - Save data to file
- `Delete()` - Delete data from file

#### UserStore

Implements ASP.NET Core Identity interfaces:

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

Uses AJIS for all data operations:
- `CreateAsync()` → Serialize + Save to users.json
- `FindByIdAsync()` → Query from users.json
- `UpdateAsync()` → Modify + Save to users.json
- `DeleteAsync()` → Remove + Save to users.json

### File Structure

```
data/
├── users.json      # All user accounts
├── roles.json      # All roles
└── profiles.json   # User profiles (optional)
```

Each file is a JSON array of objects:

```json
[
  {
    "Id": "uuid",
    "UserName": "user1",
    "Email": "user@example.com",
    ...
  }
]
```

## ASP.NET Core Identity Integration

### Services Configuration

In `Startup.cs`:

```csharp
services.AddIdentity<User, Role>(options => { ... })
    .AddUserStore<UserStore>()
    .AddRoleStore<RoleStore>();
```

### Key Interfaces Implemented

#### UserStore Implements:
- `IUserStore<TUser>`
- `IUserPasswordStore<TUser>`
- `IUserRoleStore<TUser>`
- `IUserEmailStore<TUser>`
- `IUserLockoutStore<TUser>`
- `IUserPhoneNumberStore<TUser>`
- `IUserSecurityStampStore<TUser>`

#### RoleStore Implements:
- `IRoleStore<TRole>`
- `IRoleLockoutStore<TRole>`

## Security

### Password Hashing

Passwords are hashed using ASP.NET Core Identity's default hasher:

```csharp
var passwordHasher = new PasswordHasher<User>();
user.PasswordHash = passwordHasher.HashPassword(user, password);
```

### Role-Based Authorization

```csharp
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    // Only Admins can access
}
```

### Security Stamps

Used for concurrency and invalidation:

```csharp
user.SecurityStamp = Guid.NewGuid().ToString();
```

## Concurrency

Uses optimistic concurrency with `ConcurrencyStamp`:

```csharp
user.ConcurrencyStamp = Guid.NewGuid().ToString();
```

When updating, compare the stamp to prevent overwrites.

## Testing Strategy

### Unit Tests

- UserStore CRUD operations
- RoleStore CRUD operations
- Role assignment
- Password management

### Integration Tests

- Full authentication flow
- User registration
- Login/logout
- Role assignment

## Performance Considerations

### File I/O

- Small to medium datasets (up to 100K users)
- Consider caching for larger datasets
- Use async I/O for scalability

### Query Performance

- Linear search for now
- Consider indexing for large datasets
- Use query optimization where needed

## Scalability

### Limitations

- Single file per entity type
- File-based locking
- No distributed caching

### Recommendations

- Use for small to medium applications
- Consider database for large-scale apps
- Add caching layer for performance

## Deployment

### Production Checklist

1. Set strong password requirements
2. Enable HTTPS
3. Set up log rotation
4. Configure backup for data files
5. Set appropriate file permissions
6. Enable rate limiting

### Backup Strategy

- Regular backup of `data/` directory
- Version control for configuration
- Consider cloud storage integration

## Future Enhancements

- [ ] File-based search indexing
- [ ] Incremental backups
- [ ] Compression for large files
- [ ] Database integration support
- [ ] Caching layer
- [ ] File sharding for large datasets
- [ ] Event sourcing support

## References

- [ASP.NET Core Identity](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [AJIS Documentation](https://github.com/afrowaveltd/AJIS.dotnet)
- [Custom User Stores](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/custom-user-store)
