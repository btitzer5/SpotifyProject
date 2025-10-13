using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;

namespace SpotifyProject.Pages
{
    public class BrowseModel : PageModel
    {
        private readonly SpotifyClientFactory _factory;
        private readonly ILogger<BrowseModel> _logger;

        private const string DefaultCountry = "US";

        public BrowseModel(SpotifyClientFactory factory, ILogger<BrowseModel> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public sealed class AlbumItem
        {
            public string Id { get; init; } = string.Empty;
            public string Name { get; init; } = string.Empty;
            public string? ImageUrl { get; init; }
            public string? Artist { get; init; }
        }

        public List<AlbumItem> NewReleases { get; private set; } = new();

        public async Task OnGetAsync()
        {
            var spotify = _factory.CreateAppClient();

            // New Releases only (keep page simple and reliable for now)
            try
            {
                var newReleasesResp = await spotify.Browse.GetNewReleases(new NewReleasesRequest
                {
                    Country = DefaultCountry,
                    Limit = 12
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
            catch (APIException ex)
            {
                _logger.LogWarning(ex, "Failed to load new releases");
                // Do not add model errors to keep the page clean for now
            }
        }
    }
}
