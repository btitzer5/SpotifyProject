using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyAPI.Web;

namespace SpotifyProject.Pages;

public class PlaylistsModel : PageModel
{
    private readonly SpotifyClientFactory _factory;
    public List<FullPlaylist> Playlists { get; private set; } = new();

    public PlaylistsModel(SpotifyClientFactory factory) => _factory = factory;

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var spotify = _factory.CreateUserClient();
            var page = await spotify.Playlists.CurrentUsers();
            Playlists = page.Items.ToList();
            return Page();
        }
        catch (InvalidOperationException)
        {
            // Not authenticated
            return RedirectToPage("/Auth/Login");
        }
    }
}