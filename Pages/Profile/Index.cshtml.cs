using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpotifyProject.Services;

namespace SpotifyProject.Pages.Profile
{
    public class IndexModel : PageModel
    {
        private readonly SpotifyService _spotifyService;

        public IndexModel(SpotifyService spotifyService)
        {
            _spotifyService = spotifyService;
        }

        public PrivateUser CurrentUser { get; set; }
        public Paging<FullTrack> TopTracks { get; set; }
        public Paging<FullArtist> TopArtists { get; set; }
        public List<SimpleAlbum> TopAlbums { get; set; }
        public string TimeRange { get; set; } = "medium_term";
        public int Limit { get; set; } = 10;
        public string ErrorMessage { get; set; }

        public int followersCount;

        public async Task OnGetAsync(string timeRange = "medium_term", int limit = 10)
        {
            TimeRange = timeRange;
            // Cap the limit at 50 (Spotify's maximum)
            Limit = Math.Min(limit, 50);
           
            try
            {
                // Get current user profile
                CurrentUser = await _spotifyService.GetCurrentUserProfile();

                // Get user's top tracks with the capped limit
                TopTracks = await _spotifyService.GetUserTopTracks(limit: Limit, timeRange: TimeRange);
                
                // Get user's top artists with the capped limit
                TopArtists = await _spotifyService.GetUserTopArtists(limit: Limit, timeRange: TimeRange);
                
                // Extract top albums from top tracks
                if (TopTracks?.Items != null)
                {
                    TopAlbums = TopTracks.Items
                        .Select(t => t.Album)
                        .GroupBy(a => a.Id)
                        .Select(g => g.First())
                        .Take(Limit)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load profile data: {ex.Message}";
            }
        }
    }
}
