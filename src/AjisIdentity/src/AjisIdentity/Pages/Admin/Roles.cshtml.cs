using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Afrowave.AJIS.Identity;

namespace AjisIdentity.Pages.Admin;

[Authorize(Roles = "Admin")]
public class RolesModel : PageModel
{
    private readonly RoleStore _roleStore;

    public RolesModel(RoleStore roleStore)
    {
        _roleStore = roleStore;
    }

    public IList<Role> Roles { get; set; } = new List<Role>();

    public async Task OnGetAsync()
    {
        var roles = _roleStore.GetAllRoles();
        Roles = roles;
    }
}
