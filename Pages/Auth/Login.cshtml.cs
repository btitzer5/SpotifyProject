using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyAPI.Web;

namespace SpotifyProject.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly SpotifyAuthService _auth;

    public LoginModel(SpotifyAuthService auth) => _auth = auth;

    public IActionResult OnGet()
    {
        var uri = _auth.BuildLoginUri(new[]
        {
            Scopes.PlaylistReadPrivate,
            Scopes.PlaylistReadCollaborative,
            Scopes.UserReadEmail,
            Scopes.UserLibraryRead,
            Scopes.UserReadPrivate,        // Required for user profile data
            Scopes.UserTopRead             // Required for top tracks and artists
        });

        return Redirect(uri.ToString());
    }
}