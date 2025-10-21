using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using SpotifyProject.Services;

namespace SpotifyProject.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly SpotifyClientFactory _factory;
        private readonly SpotifyService _spotifyService;

        private const string DefaultCountry = "US";

        public IndexModel(ILogger<IndexModel> logger,
                          SpotifyClientFactory factory,
                          SpotifyService spotifyService)
        {
            _logger = logger;
            _factory = factory;
            _spotifyService = spotifyService;
        }

        public sealed class AlbumItem
        {
            public string Id { get; init; } = string.Empty;
            public string Name { get; init; } = string.Empty;
            public string? ImageUrl { get; init; }
            public string? Artist { get; init; }
        }

        public List<AlbumItem> NewReleases { get; private set; } = new();
        public List<FullArtist> TopArtists { get; private set; } = new();
        public List<FullTrack> TopTracks { get; private set; } = new();
        public bool IsAuthenticated { get; private set; }

        public async Task OnGetAsync()
        {
            IsAuthenticated = true;
            try
            {
                // Basic new releases (app client)
                var app = _factory.CreateAppClient();
                var newReleasesResp = await app.Browse.GetNewReleases(new NewReleasesRequest
                {
                    Country = DefaultCountry,
                    Limit = 8
                });

                var albums = newReleasesResp?.Albums?.Items?.ToList() ?? new List<SimpleAlbum>();
                NewReleases = albums.Select(a => new AlbumItem
                {
                    Id = a.Id,
                    Name = a.Name,
                    Artist = a.Artists?.FirstOrDefault()?.Name,
                    ImageUrl = a.Images?.OrderByDescending(img => img.Width).FirstOrDefault()?.Url
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load new releases");
            }

            // Try to load user-specific quick data — skip if not authenticated
            try
            {
                var topArtistsResp = await _spotifyService.GetUserTopArtists(limit: 6, timeRange: "medium_term");
                TopArtists = topArtistsResp?.Items?.ToList() ?? new List<FullArtist>();
            }
            catch (InvalidOperationException)
            {
                // not authenticated (CreateUserClient throws)
                IsAuthenticated = false;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to load top artists");
            }

            try
            {
                var topTracksResp = await _spotifyService.GetUserTopTracks(limit: 6, timeRange: "medium_term");
                TopTracks = topTracksResp?.Items?.ToList() ?? new List<FullTrack>();
            }
            catch
            {
                // ignore - either not authenticated or API error
            }
        }
    }
}
