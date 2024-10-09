using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ScaryCavesWeb.Pages;

public class Login : PageModel
{
    [BindProperty, Required] public string PlayerName { get; set; } = "";

    [BindProperty] public string Password { get; set; } = "";

    public IActionResult OnPost()
    {
        // see if these credentials work
        // if so, put the player in the room they're supposed to be at
        // otherwise, error.
        if (!string.IsNullOrEmpty(PlayerName))
        {
            return RedirectToPage("/room/{id}", new { id = 0 });
        }

        // invalid
        return Page();
    }
}
