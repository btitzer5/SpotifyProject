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
        public string TimeRange { get; set; } = "medium_term"; // Options: short_term, medium_term, long_term
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync(string timeRange = "medium_term")
        {
            TimeRange = timeRange;
            
            try
            {
                // Get current user profile
                CurrentUser = await _spotifyService.GetCurrentUserProfile();
                
                // Get user's top tracks
                TopTracks = await _spotifyService.GetUserTopTracks(limit: 10, timeRange: TimeRange);
                
                // Get user's top artists
                TopArtists = await _spotifyService.GetUserTopArtists(limit: 10, timeRange: TimeRange);
                
                // Extract top albums from top tracks
                if (TopTracks?.Items != null)
                {
                    TopAlbums = TopTracks.Items
                        .Select(t => t.Album)
                        .GroupBy(a => a.Id)
                        .Select(g => g.First())
                        .Take(10)
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
