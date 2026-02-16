using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Afrowave.AJIS.Identity;

namespace AjisIdentity.Pages.Account;

public class LoginModel : PageModel
{
    private readonly UserStore _userStore;

    public LoginModel(UserStore userStore)
    {
        _userStore = userStore;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public bool RememberMe { get; set; }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userStore.FindByNameAsync(Input.UserName ?? string.Empty, CancellationToken.None);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }

        return RedirectToPage("/Index");
    }
}
