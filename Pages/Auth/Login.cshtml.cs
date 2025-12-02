using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyAPI.Web;

namespace SpotifyProject.Pages.Auth
{

    public class LoginModel : PageModel
    {
        private readonly SpotifyAuthService _auth;

    public LoginModel(SpotifyAuthService auth) => _auth = auth;

        public IActionResult OnGet()
        {
            var uri = _auth.BuildLoginUri(new[]
            {
                // Playlists and basic profile
                Scopes.PlaylistReadPrivate,
                Scopes.PlaylistReadCollaborative,
                Scopes.UserReadEmail,
                Scopes.UserLibraryRead,
                Scopes.UserReadPrivate,        // profile data
                Scopes.UserTopRead,            // top tracks and artists

                // NEW: required for chatbot features
                Scopes.UserReadRecentlyPlayed, // "recently played"
                Scopes.UserReadPlaybackState,  // playback status
                Scopes.UserReadCurrentlyPlaying // "currently playing"
            });

            return Redirect(uri.ToString());
        }
    }
}
