using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Afrowave.AJIS.Identity;

namespace AjisIdentity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserStore _userStore;
    private readonly RoleStore _roleStore;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public RegisterModel(UserStore userStore, RoleStore roleStore)
    {
        _userStore = userStore;
        _roleStore = roleStore;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new User
        {
            Email = Input.Email,
            UserName = Input.UserName,
            Profile = new UserProfile
            {
                FirstName = Input.FirstName,
                LastName = Input.LastName
            }
        };

        await _userStore.CreateAsync(user, CancellationToken.None);

        return RedirectToPage("/Index");
    }
}
