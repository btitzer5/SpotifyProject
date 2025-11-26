using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyAPI.Web;
using SpotifyProject.Services;
using System.Threading.Tasks;
using System.Linq;

namespace SpotifyProject.Pages.Track
{
    public class IndexModel : PageModel
    {
        private readonly SpotifyService _spotify;

        public IndexModel(SpotifyService spotify) => _spotify = spotify;

        public FullTrack? Track { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            Track = await _spotify.GetTrack(id);

            return Page();
        }
    }
}
