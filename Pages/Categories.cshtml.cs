using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;

namespace SpotifyProject.Pages
{
    public class CategoriesModel : PageModel
    {
        private readonly SpotifyClientFactory _factory;
        private readonly ILogger<CategoriesModel> _logger;

        private const string DefaultCountry = "US"; // Ensure consistent market for categories and playlists
        private static readonly string?[] CountryFallbacks = new string?[] { DefaultCountry, null, "GB", "CA" };

        public sealed class CategoryItem
        {
            public string Id { get; init; } = "";
            public string Name { get; init; } = "";
        }

        public List<CategoryItem> AllCategories { get; private set; } = new();
        public List<string> Selected { get; set; } = new();
        public Dictionary<string, List<FullPlaylist>> PlaylistsByCategory { get; private set; } = new();

        public CategoriesModel(SpotifyClientFactory factory, ILogger<CategoriesModel> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        // Support optional selected ids in querystring for GET navigation from Browse page
        public async Task OnGetAsync(List<string>? selected = null)
        {
            await LoadCategoriesAsync();

            if (selected is { Count: > 0 })
            {
                await LoadPlaylistsForSelectionAsync(selected);
            }
        }

        public async Task OnPostAsync(List<string>? selected)
        {
            // Always reload categories so we can render names in the view
            await LoadCategoriesAsync();

            if (selected is { Count: > 0 })
            {
                await LoadPlaylistsForSelectionAsync(selected);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "No valid categories selected.");
            }
        }

        private async Task LoadPlaylistsForSelectionAsync(List<string> selected)
        {
            var spotify = _factory.CreateAppClient();

            // Validate selection
            var allowedIds = new HashSet<string>(AllCategories.Select(c => c.Id), StringComparer.OrdinalIgnoreCase);
            Selected = (selected ?? new())
                .Where(id => allowedIds.Contains(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

            if (Selected.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "No valid categories selected.");
                return;
            }

            // Fetch playlists for each selected category
            foreach (var categoryId in Selected)
            {
                try
                {
                    var items = await FetchPlaylistsWithFallbackAsync(spotify, categoryId);
                    PlaylistsByCategory[categoryId] = items;
                }
                catch (APIUnauthorizedException ex)
                {
                    ModelState.AddModelError(string.Empty, "Unauthorized. Check Spotify:ClientId/ClientSecret.");
                    _logger.LogError(ex, "Unauthorized while fetching playlists for category {CategoryId}", categoryId);
                }
                catch (APITooManyRequestsException ex)
                {
                    ModelState.AddModelError(string.Empty, $"Spotify rate limit hit. Retry after {ex.RetryAfter.TotalSeconds:N0}s.");
                    _logger.LogWarning(ex, "Rate limited while fetching playlists for category {CategoryId}", categoryId);
                }
                catch (APIException ex)
                {
                    var status = (int?)ex.Response?.StatusCode;
                    if (status == 404)
                    {
                        _logger.LogInformation("No playlists found for category {CategoryId} in any tested market", categoryId);
                        PlaylistsByCategory[categoryId] = new List<FullPlaylist>();
                        continue;
                    }
                    _logger.LogError(ex, "Spotify error while fetching playlists for category {CategoryId}. Status: {Status}", categoryId, status);
                    ModelState.AddModelError(string.Empty, $"Spotify error while fetching playlists for category '{categoryId}': {status}. {ex.Message}");
                }
            }
        }

        private async Task<List<FullPlaylist>> FetchPlaylistsWithFallbackAsync(SpotifyClient spotify, string categoryId)
        {
            // Try category playlists across fallbacks and multiple pages
            foreach (var country in CountryFallbacks)
            {
                try
                {
                    var aggregated = new List<FullPlaylist>();

                    for (int offset = 0; offset < 100; offset += 50)
                    {
                        var request = new CategoriesPlaylistsRequest { Limit = 50, Offset = offset };
                        if (!string.IsNullOrWhiteSpace(country))
                        {
                            request.Country = country;
                        }

                        var resp = await spotify.Browse.GetCategoryPlaylists(categoryId, request);
                        var items = resp?.Playlists?.Items?.ToList() ?? new List<FullPlaylist>();
                        if (items.Count == 0)
                        {
                            break; // no more pages for this market
                        }

                        aggregated.AddRange(items);

                        // if fewer than requested returned, we've reached the end
                        if (items.Count < request.Limit)
                        {
                            break;
                        }
                    }

                    if (aggregated.Count > 0)
                    {
                        if (country != DefaultCountry)
                        {
                            _logger.LogInformation("Fetched {Count} playlists for category {CategoryId} using market override {Country}", aggregated.Count, categoryId, country ?? "<none>");
                        }
                        return aggregated;
                    }

                    _logger.LogDebug("No playlists returned for category {CategoryId} with market {Country}; trying next fallback", categoryId, country ?? "<none>");
                }
                catch (APIException ex) when ((int?)ex.Response?.StatusCode == 404)
                {
                    _logger.LogDebug(ex, "404 for category {CategoryId} with market {Country}; trying next fallback", categoryId, country ?? "<none>");
                    // Try next fallback
                }
            }

            return new List<FullPlaylist>();
        }

        private async Task LoadCategoriesAsync()
        {
            var spotify = _factory.CreateAppClient();

            try
            {
                var response = await spotify.Browse.GetCategories(new CategoriesRequest
                {
                    Country = DefaultCountry,
                    Limit = 50
                });
                var page = response?.Categories;

                AllCategories = page?.Items?
                    .Where(i => !string.IsNullOrWhiteSpace(i.Id))
                    .Select(i => new CategoryItem { Id = i.Id!, Name = i.Name ?? i.Id! })
                    .OrderBy(i => i.Name)
                    .ToList()
                    ?? new();
            }
            catch (APIUnauthorizedException ex)
            {
                ModelState.AddModelError(string.Empty, "Unauthorized. Check Spotify:ClientId/ClientSecret.");
                _logger.LogError(ex, "Unauthorized while loading categories");
            }
            catch (APITooManyRequestsException ex)
            {
                ModelState.AddModelError(string.Empty, $"Spotify rate limit hit while loading categories. Retry after {ex.RetryAfter.TotalSeconds:N0}s.");
                _logger.LogWarning(ex, "Rate limited while loading categories");
            }
            catch (APIException ex)
            {
                var status = (int?)ex.Response?.StatusCode;
                _logger.LogError(ex, "Spotify error while loading categories. Status: {Status}", status);
                ModelState.AddModelError(string.Empty, $"Spotify error while loading categories: {status}. {ex.Message}");
            }
        }
    }
}
