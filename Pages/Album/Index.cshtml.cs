using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyAPI.Web;
using SpotifyProject.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpotifyProject.Pages.Album
{
    public class IndexModel : PageModel
    {
        private readonly SpotifyService _spotify;

        public IndexModel(SpotifyService spotify) => _spotify = spotify;

        public FullAlbum? Album { get; set; }
        public List<SimpleTrack> Tracks { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            Album = await _spotify.GetAlbum(id);
            Tracks = await _spotify.GetAlbumTracks(id);

            return Page();
        }
    }
}