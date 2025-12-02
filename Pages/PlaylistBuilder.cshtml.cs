using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyAPI.Web;
using SpotifyProject.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpotifyProject.Pages
{
    public class PlaylistBuilderModel : PageModel
    {
        private readonly SpotifyService _spotifyService;
        private const string SessionKey = "PlaylistBuilder_SelectedTracks";

        public PlaylistBuilderModel(SpotifyService spotifyService)
        {
            _spotifyService = spotifyService;
        }

        [BindProperty]
        public string SearchQuery { get; set; }

        public SearchResponse SearchResults { get; set; }

        public List<SelectedTrack> SelectedTracks { get; set; } = new();

        public List<SelectedTrack> RecommendedTracks { get; set; } = new();

        [BindProperty]
        public bool IsPublic { get; set; }

        public class SelectedTrack
        {
            public string Uri { get; set; }
            public string Name { get; set; }
            public string Artist { get; set; }
            public string Image { get; set; }
        }

        private void LoadSession()
        {
            var json = HttpContext.Session.GetString(SessionKey);
            if (!string.IsNullOrEmpty(json))
                SelectedTracks = JsonSerializer.Deserialize<List<SelectedTrack>>(json);
        }

        private void SaveSession()
        {
            var json = JsonSerializer.Serialize(SelectedTracks);
            HttpContext.Session.SetString(SessionKey, json);
        }

        public void OnGet()
        {
            LoadSession();
        }

        public async Task<IActionResult> OnPostSearch()
        {
            LoadSession();

            if (!string.IsNullOrEmpty(SearchQuery))
                SearchResults = await _spotifyService.SearchTracksAsync(SearchQuery, 20);

            return Page();
        }

        public IActionResult OnPostAddTrack(string trackUri, string trackName, string trackArtist, string trackImage)
        {
            LoadSession();

            if (!SelectedTracks.Any(t => t.Uri == trackUri))
            {
                SelectedTracks.Add(new SelectedTrack
                {
                    Uri = trackUri,
                    Name = trackName,
                    Artist = trackArtist,
                    Image = trackImage
                });
            }

            SaveSession();
            return RedirectToPage();
        }

        public IActionResult OnPostRemoveTrack(string trackUri)
        {
            LoadSession();

            SelectedTracks.RemoveAll(t => t.Uri == trackUri);

            SaveSession();
            return RedirectToPage();
        }

        public IActionResult OnPostClearAll()
        {
            LoadSession();

            SelectedTracks.Clear();

            SaveSession();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddAllTopTracks()
        {
            LoadSession();

            var topTracks = await _spotifyService.GetUserTopTracks(10, "short_term");

            foreach (var track in topTracks.Items)
            {
                if (!SelectedTracks.Any(t => t.Uri == track.Uri))
                {
                    SelectedTracks.Add(new SelectedTrack
                    {
                        Uri = track.Uri,
                        Name = track.Name,
                        Artist = string.Join(", ", track.Artists.Select(a => a.Name)),
                        Image = track.Album.Images.FirstOrDefault()?.Url
                    });
                }
            }

            SaveSession();
            return RedirectToPage();
        }

        // recommend tracks based on the ones selected
        public async Task<IActionResult> OnPostRecommend()
        {
            LoadSession();
            RecommendedTracks = new List<SelectedTrack>();

            if (!SelectedTracks.Any())
            {
                return Page();
            }

            var firstArtist = SelectedTracks.First().Artist?.Split(',').FirstOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(firstArtist))
                return Page();

            var results = await _spotifyService.SearchTracksAsync(firstArtist, 20);

            if (results?.Tracks?.Items != null)
            {
                var existingUris = SelectedTracks.Select(t => t.Uri).ToHashSet();

                var recs = results.Tracks.Items
                    .Where(t => !existingUris.Contains(t.Uri))
                    .Take(10)
                    .ToList();

                RecommendedTracks = recs.Select(track => new SelectedTrack
                {
                    Uri = track.Uri,
                    Name = track.Name,
                    Artist = string.Join(", ", track.Artists.Select(a => a.Name)),
                    Image = track.Album.Images.FirstOrDefault()?.Url
                }).ToList();
            }

            return Page();
        }

        // creating an autocomplete endpoint for searching tracks
        public async Task<JsonResult> OnGetAutocomplete(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new JsonResult(new List<object>());

            var results = await _spotifyService.SearchTracksAsync(query, 5);

            var simplified = results.Tracks.Items.Select(t => new
            {
                name = t.Name,
                artist = string.Join(", ", t.Artists.Select(a => a.Name)),
                id = t.Id
            });

            return new JsonResult(simplified);
        }

        public async Task<IActionResult> OnPostCreatePlaylist(string playlistName, string playlistDescription)
        {
            LoadSession();

            if (string.IsNullOrWhiteSpace(playlistName))
                return RedirectToPage();

            // Pass IsPublic into SpotifyService so toggle actually does something
            var playlist = await _spotifyService.CreatePlaylistAsync(playlistName, playlistDescription, IsPublic);

            if (SelectedTracks.Any())
            {
                var uris = SelectedTracks.Select(t => t.Uri).ToList();
                await _spotifyService.AddTracksToPlaylistAsync(playlist.Id, uris);
            }

            HttpContext.Session.Remove(SessionKey);

            return RedirectToPage("/Playlists");
        }
    }
}

