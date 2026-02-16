using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Afrowave.AJIS.Identity;
using PageModel = Microsoft.AspNetCore.Mvc.RazorPages.PageModel;

namespace AjisIdentity.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AjisContext _ajisContext;

    public IndexModel(AjisContext ajisContext)
    {
        _ajisContext = ajisContext;
    }

    public int TotalUsers { get; set; } = 0;
    public int TotalRoles { get; set; } = 0;
    public string DataPath { get; set; } = string.Empty;
    public bool UsersFileExists { get; set; } = false;
    public bool RolesFileExists { get; set; } = false;
    public bool ProfilesFileExists { get; set; } = false;
    public string AjisCoreVersion { get; set; } = "1.1.0";
    public string AjisIoVersion { get; set; } = "1.1.0";

    public void OnGet()
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, "data");
        DataPath = basePath;

        Directory.CreateDirectory(basePath);

        TotalUsers = _ajisContext.Query<User>("users.json").ToList().Count;
        TotalRoles = _ajisContext.Query<Role>("roles.json").ToList().Count;

        UsersFileExists = System.IO.File.Exists(Path.Combine(basePath, "users.json"));
        RolesFileExists = System.IO.File.Exists(Path.Combine(basePath, "roles.json"));
        ProfilesFileExists = System.IO.File.Exists(Path.Combine(basePath, "profiles.json"));
    }
}
