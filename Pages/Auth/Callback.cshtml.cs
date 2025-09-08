using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SpotifyProject.Pages.Auth;

public class CallbackModel : PageModel
{
    private readonly SpotifyAuthService _auth;

    public CallbackModel(SpotifyAuthService auth) => _auth = auth;

    public async Task<IActionResult> OnGetAsync(string? code, string? error, string? state)
    {
        if (!string.IsNullOrEmpty(error)) return BadRequest(error);
        if (string.IsNullOrEmpty(code)) return BadRequest("Missing authorization code.");

        await _auth.StoreTokensFromCallbackAsync(code, state);
        return RedirectToPage("/Playlists");
    }
}