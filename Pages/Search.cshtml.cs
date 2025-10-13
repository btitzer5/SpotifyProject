using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyProject.Models;
using SpotifyProject.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpotifyProject.Pages
{
    public class SearchModel : PageModel
    {
        private readonly ISpotifySearchService _searchService;

        public SearchResultsViewModel SearchResults { get; private set; } = new();
        public List<string> AvailableGenres { get; private set; } = new();
        public bool ShowAdvancedSearch { get; set; }

        public SearchModel(ISpotifySearchService searchService)
        {
            _searchService = searchService;
        }

        public async Task OnGetAsync(
            string? q,
            string? searchType,
            string? genre,
            int? year,
            int? fromYear,
            int? toYear,
            string? artist,
            string? album,
            string? track,
            bool isExplicit = false,
            string? market = "Any",
            string? popularity = "Any",
            int limit = 20,
            bool advanced = false)
        {
            ShowAdvancedSearch = advanced;
            
            // Load available genres for the dropdown
            AvailableGenres = await _searchService.GetAvailableGenresAsync();

            var request = new AdvancedSearchRequest
            {
                Query = q,
                SearchType = Enum.TryParse<SearchType>(searchType, true, out var type) ? type : SearchType.All,
                Genre = genre,
                Year = year,
                FromYear = fromYear,
                ToYear = toYear,
                Artist = artist,
                Album = album,
                Track = track,
                IsExplicit = isExplicit,
                Market = Enum.TryParse<MarketFilter>(market, true, out var marketFilter) ? marketFilter : MarketFilter.Any,
                Popularity = Enum.TryParse<PopularityFilter>(popularity, true, out var popFilter) ? popFilter : PopularityFilter.Any,
                Limit = Math.Max(1, Math.Min(limit, 50))
            };

            SearchResults = await _searchService.SearchAsync(request);
        }
    }
}