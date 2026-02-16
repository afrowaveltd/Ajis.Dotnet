using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Afrowave.AJIS.Identity;

namespace AjisIdentity.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly UserStore _userStore;

    public UsersModel(UserStore userStore)
    {
        _userStore = userStore;
    }

    public IList<User> Users { get; set; } = new List<User>();

    public async Task OnGetAsync()
    {
        var users = _userStore.GetAllUsers();
        Users = users;
    }
}
