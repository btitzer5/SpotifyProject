using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SpotifyProject.Services
{
    public class SpotifyService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SpotifyClientFactory _spotifyClientFactory;

        public SpotifyService(IHttpContextAccessor httpContextAccessor, SpotifyClientFactory spotifyClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _spotifyClientFactory = spotifyClientFactory;
        }

        // App-only client (public endpoints)
        private SpotifyClient GetAppClient() => _spotifyClientFactory.CreateAppClient();

        // User-authenticated client (private endpoints) — will throw if user not authenticated
        private SpotifyClient GetUserClient() => _spotifyClientFactory.CreateUserClient();

        // USER-SPECIFIC endpoints (require authenticated user)
        public async Task<PrivateUser> GetCurrentUserProfile()
        {
            var spotify = GetUserClient();
            return await spotify.UserProfile.Current();
        }

        public async Task<Paging<FullTrack>> GetUserTopTracks(int limit = 10, string timeRange = "medium_term")
        {
            var spotify = GetUserClient();

            try
            {
                var request = new PersonalizationTopRequest();
                request.Limit = limit;
                var requestType = request.GetType();
                var properties = requestType.GetProperties();

                foreach (var prop in properties)
                {
                    if (prop.Name.ToLower().Contains("time") || prop.Name.ToLower().Contains("range"))
                    {
                        try
                        {
                            if (prop.PropertyType == typeof(string))
                            {
                                prop.SetValue(request, timeRange);
                            }
                            else if (prop.PropertyType.IsEnum || (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && prop.PropertyType.GetGenericArguments()[0].IsEnum))
                            {
                                var enumType = prop.PropertyType.IsGenericType ? prop.PropertyType.GetGenericArguments()[0] : prop.PropertyType;
                                var enumNames = Enum.GetNames(enumType);
                                var matchingEnum = enumNames.FirstOrDefault(name =>
                                    name.ToLower().Contains(timeRange.Replace("_", "").Replace("-", "")));

                                if (matchingEnum != null)
                                {
                                    var enumValue = Enum.Parse(enumType, matchingEnum);
                                    prop.SetValue(request, enumValue);
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                return await spotify.Personalization.GetTopTracks(request);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting top tracks for {timeRange}: {ex.Message}", ex);
            }
        }

        public async Task<Paging<FullArtist>> GetUserTopArtists(int limit = 10, string timeRange = "medium_term")
        {
            var spotify = GetUserClient();

            try
            {
                var request = new PersonalizationTopRequest();
                request.Limit = limit;
                var requestType = request.GetType();
                var properties = requestType.GetProperties();

                foreach (var prop in properties)
                {
                    if (prop.Name.ToLower().Contains("time") || prop.Name.ToLower().Contains("range"))
                    {
                        try
                        {
                            if (prop.PropertyType == typeof(string))
                            {
                                prop.SetValue(request, timeRange);
                            }
                            else if (prop.PropertyType.IsEnum || (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && prop.PropertyType.GetGenericArguments()[0].IsEnum))
                            {
                                var enumType = prop.PropertyType.IsGenericType ? prop.PropertyType.GetGenericArguments()[0] : prop.PropertyType;
                                var enumNames = Enum.GetNames(enumType);
                                var matchingEnum = enumNames.FirstOrDefault(name =>
                                    name.ToLower().Contains(timeRange.Replace("_", "").Replace("-", "")));

                                if (matchingEnum != null)
                                {
                                    var enumValue = Enum.Parse(enumType, matchingEnum);
                                    prop.SetValue(request, enumValue);
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                return await spotify.Personalization.GetTopArtists(request);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting top artists for {timeRange}: {ex.Message}", ex);
            }
        }

        // Get current user's playlists (requires authenticated user)
        public async Task<Paging<FullPlaylist>> GetCurrentUserPlaylists(int limit = 50, int offset = 0)
        {
            var spotify = GetUserClient();
            // Use the Playlists.CurrentUsers request to fetch the signed-in user's playlists
            var request = new PlaylistCurrentUsersRequest
            {
                Limit = Math.Min(limit, 50),
                Offset = Math.Max(offset, 0)
            };
            return await spotify.Playlists.CurrentUsers(request);
        }

        // ---------- Public app-level methods (no user token required) ----------
        public async Task<FullArtist> GetArtist(string artistId)
        {
            var spotify = GetAppClient();
            return await spotify.Artists.Get(artistId);
        }

        public async Task<List<SimpleAlbum>> GetArtistAlbums(string artistId, int limit = 50)
        {
            var spotify = GetAppClient();
            var response = await spotify.Artists.GetAlbums(artistId, new ArtistsAlbumsRequest { Limit = Math.Min(limit, 50) });
            return response.Items.ToList();
        }

        public async Task<FullAlbum> GetAlbum(string albumId)
        {
            var spotify = GetAppClient();
            return await spotify.Albums.Get(albumId);
        }

        public async Task<List<SimpleTrack>> GetAlbumTracks(string albumId, int limit = 50)
        {
            var spotify = GetAppClient();
            var response = await spotify.Albums.GetTracks(albumId, new AlbumTracksRequest { Limit = Math.Min(limit, 50) });
            return response.Items.ToList();
        }

        public async Task<FullTrack> GetTrack(string trackId)
        {
            var spotify = GetAppClient();
            return await spotify.Tracks.Get(trackId);
        }

        public async Task<FullPlaylist> GetPlaylist(string playlistId)
        {
            var spotify = GetAppClient();
            return await spotify.Playlists.Get(playlistId);
        }

        public async Task<List<FullTrack>> GetPlaylistTracks(string playlistId, int limit = 100)
        {
            var spotify = GetAppClient();
            var response = await spotify.Playlists.GetItems(playlistId, new PlaylistGetItemsRequest { Limit = Math.Min(limit, 100) });
            var tracks = response.Items
                .Where(i => i.Track is FullTrack)
                .Select(i => (FullTrack)i.Track!)
                .ToList();
            return tracks;
        }

        // Get current user's Spotify ID
        public async Task<string> GetCurrentUserIdAsync()
        {
            var user = await GetCurrentUserProfile();
            return user.Id;
        }

        // Create a new Playlist using new SpotifyClient
        public async Task<FullPlaylist> CreatePlaylistAsync(string name, string description = "", bool isPublic = false)
        {
            var spotify = GetUserClient();
            var userId = await GetCurrentUserIdAsync();

            var request = new PlaylistCreateRequest(name)
            {
                Description = description,
                Public = isPublic
            };

            return await spotify.Playlists.Create(userId, request);
        }

        // Add tracks to a playlist (expects Spotify URIs like spotify:track:xxxx)
        public async Task AddTracksToPlaylistAsync(string playlistId, List<string> trackUris)
        {
            var spotify = GetUserClient();

            var request = new PlaylistAddItemsRequest(trackUris);
            await spotify.Playlists.AddItems(playlistId, request);
        }

        // Search tracks for Playlist Builder
        public async Task<SearchResponse> SearchTracksAsync(string query, int limit = 20)
        {
            var spotify = GetUserClient();

            var req = new SearchRequest(SearchRequest.Types.Track, query)
            {
                Limit = limit
            };

            return await spotify.Search.Item(req);
        }


    }
}