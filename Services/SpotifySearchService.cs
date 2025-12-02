using SpotifyAPI.Web;
using SpotifyProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyProject.Services
{
    public class SpotifySearchService : ISpotifySearchService
    {
        private readonly SpotifyClientFactory _factory;

        public SpotifySearchService(SpotifyClientFactory factory)
        {
            _factory = factory;
        }

        public async Task<SearchResultsViewModel> SearchAsync(AdvancedSearchRequest request)
        {
            var result = new SearchResultsViewModel { SearchRequest = request };

            try
            {
                if (string.IsNullOrWhiteSpace(request.Query) && 
                    string.IsNullOrWhiteSpace(request.Artist) && 
                    string.IsNullOrWhiteSpace(request.Album) && 
                    string.IsNullOrWhiteSpace(request.Track))
                {
                    return result;
                }

                var spotify = _factory.CreateAppClient();
                var searchQuery = BuildSearchQuery(request);
                var searchTypes = GetSearchTypes(request.SearchType);

                var searchRequest = new SearchRequest(searchTypes, searchQuery)
                {
                    Limit = Math.Min(request.Limit, 50),
                    Market = request.Market != MarketFilter.Any ? request.Market.ToString() : null
                };

                var searchResult = await spotify.Search.Item(searchRequest);

                // Filter and populate results
                if (searchResult.Artists?.Items != null && searchTypes.HasFlag(SearchRequest.Types.Artist))
                {
                    result.Artists = FilterArtistsByPopularity(searchResult.Artists.Items.ToList(), request.Popularity);
                }

                if (searchResult.Tracks?.Items != null && searchTypes.HasFlag(SearchRequest.Types.Track))
                {
                    result.Tracks = FilterTracksByPopularity(searchResult.Tracks.Items.ToList(), request.Popularity);
                }

                if (searchResult.Albums?.Items != null && searchTypes.HasFlag(SearchRequest.Types.Album))
                {
                    result.Albums = searchResult.Albums.Items.ToList();
                }

                if (searchResult.Playlists?.Items != null && searchTypes.HasFlag(SearchRequest.Types.Playlist))
                {
                    result.Playlists = searchResult.Playlists.Items.ToList();
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Search failed: {ex.Message}";
            }

            return result;
        }

        public async Task<List<string>> GetAvailableGenresAsync()
        {
            try
            {
                var spotify = _factory.CreateAppClient();
                // Use the correct method name - GetRecommendationGenres not GetAvailableGenreSeeds
                var genres = await spotify.Browse.GetRecommendationGenres();
                return genres.Genres.OrderBy(g => g).ToList();
            }
            catch
            {
                // Return common genres as fallback
                return new List<string>
                {
                    "pop", "rock", "hip-hop", "jazz", "classical", "electronic", "country", 
                    "blues", "reggae", "folk", "punk", "metal", "indie", "alternative", "r-n-b"
                };
            }
        }

        private string BuildSearchQuery(AdvancedSearchRequest request)
        {
            var queryParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                queryParts.Add(request.Query);
            }

            if (!string.IsNullOrWhiteSpace(request.Artist))
            {
                queryParts.Add($"artist:\"{request.Artist}\"");
            }

            if (!string.IsNullOrWhiteSpace(request.Album))
            {
                queryParts.Add($"album:\"{request.Album}\"");
            }

            if (!string.IsNullOrWhiteSpace(request.Track))
            {
                queryParts.Add($"track:\"{request.Track}\"");
            }

            if (!string.IsNullOrWhiteSpace(request.Genre))
            {
                queryParts.Add($"genre:\"{request.Genre}\"");
            }

            if (request.Year.HasValue)
            {
                queryParts.Add($"year:{request.Year}");
            }
            else if (request.FromYear.HasValue || request.ToYear.HasValue)
            {
                var fromYear = request.FromYear ?? 1900;
                var toYear = request.ToYear ?? DateTime.Now.Year;
                queryParts.Add($"year:{fromYear}-{toYear}");
            }

            return string.Join(" ", queryParts);
        }

        private SearchRequest.Types GetSearchTypes(SearchType searchType)
        {
            return searchType switch
            {
                SearchType.Artists => SearchRequest.Types.Artist,
                SearchType.Tracks => SearchRequest.Types.Track,
                SearchType.Albums => SearchRequest.Types.Album,
                SearchType.Playlists => SearchRequest.Types.Playlist,
                _ => SearchRequest.Types.Artist | SearchRequest.Types.Track | SearchRequest.Types.Album | SearchRequest.Types.Playlist
            };
        }

        private List<FullArtist> FilterArtistsByPopularity(List<FullArtist> artists, PopularityFilter popularity)
        {
            return popularity switch
            {
                PopularityFilter.Low => artists.Where(a => a.Popularity <= 33).ToList(),
                PopularityFilter.Medium => artists.Where(a => a.Popularity > 33 && a.Popularity <= 66).ToList(),
                PopularityFilter.High => artists.Where(a => a.Popularity > 66).ToList(),
                _ => artists
            };
        }

        private List<FullTrack> FilterTracksByPopularity(List<FullTrack> tracks, PopularityFilter popularity)
        {
            return popularity switch
            {
                PopularityFilter.Low => tracks.Where(t => t.Popularity <= 33).ToList(),
                PopularityFilter.Medium => tracks.Where(t => t.Popularity > 33 && t.Popularity <= 66).ToList(),
                PopularityFilter.High => tracks.Where(t => t.Popularity > 66).ToList(),
                _ => tracks
            };
        }
    }
}
