using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyAPI.Web;
using SpotifyProject.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpotifyProject.Pages.Playlist
{
    public class IndexModel : PageModel
    {
        private readonly SpotifyService _spotify;

        public IndexModel(SpotifyService spotify) => _spotify = spotify;

        public FullPlaylist? Playlist { get; set; }
        public List<FullTrack> Tracks { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            Playlist = await _spotify.GetPlaylist(id);
            Tracks = await _spotify.GetPlaylistTracks(id);

            return Page();
        }
    }
}