using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Afrowave.AJIS.Identity;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddUserStore<UserStore>()
.AddRoleStore<RoleStore>()
.AddDefaultTokenProviders();

builder.Services.AddSingleton<AjisContext>(sp =>
{
    var basePath = Path.Combine(AppContext.BaseDirectory, "data");
    Directory.CreateDirectory(basePath);
    return new AjisContext(basePath);
});

builder.Services.AddSingleton<UserStore>(sp =>
{
    var basePath = Path.Combine(AppContext.BaseDirectory, "data");
    return new UserStore(basePath);
});

builder.Services.AddSingleton<RoleStore>(sp =>
{
    var basePath = Path.Combine(AppContext.BaseDirectory, "data");
    return new RoleStore(basePath);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
