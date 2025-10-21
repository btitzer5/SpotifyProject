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

        private SpotifyClient GetSpotifyClient()
        {
            return _spotifyClientFactory.CreateAppClient();
        }

        public async Task<PrivateUser> GetCurrentUserProfile()
        {
            var spotify = GetSpotifyClient();
            return await spotify.UserProfile.Current();
        }

        public async Task<Paging<FullTrack>> GetUserTopTracks(int limit = 10, string timeRange = "medium_term")
        {
            var spotify = GetSpotifyClient();
            
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
            var spotify = GetSpotifyClient();
            
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

        // ---------- New convenience read methods for navigation pages ----------

        public async Task<FullArtist> GetArtist(string artistId)
        {
            var spotify = GetSpotifyClient();
            return await spotify.Artists.Get(artistId);
        }

        public async Task<List<SimpleAlbum>> GetArtistAlbums(string artistId, int limit = 50)
        {
            var spotify = GetSpotifyClient();
            var response = await spotify.Artists.GetAlbums(artistId, new ArtistsAlbumsRequest { Limit = Math.Min(limit, 50) });
            return response.Items.ToList();
        }

        public async Task<FullAlbum> GetAlbum(string albumId)
        {
            var spotify = GetSpotifyClient();
            return await spotify.Albums.Get(albumId);
        }

        public async Task<List<SimpleTrack>> GetAlbumTracks(string albumId, int limit = 50)
        {
            var spotify = GetSpotifyClient();
            var response = await spotify.Albums.GetTracks(albumId, new AlbumTracksRequest { Limit = Math.Min(limit, 50) });
            return response.Items.ToList();
        }

        public async Task<FullTrack> GetTrack(string trackId)
        {
            var spotify = GetSpotifyClient();
            return await spotify.Tracks.Get(trackId);
        }

        public async Task<FullPlaylist> GetPlaylist(string playlistId)
        {
            var spotify = GetSpotifyClient();
            return await spotify.Playlists.Get(playlistId);
        }

        public async Task<List<FullTrack>> GetPlaylistTracks(string playlistId, int limit = 100)
        {
            var spotify = GetSpotifyClient();
            var response = await spotify.Playlists.GetItems(playlistId, new PlaylistGetItemsRequest { Limit = Math.Min(limit, 100) });
            var tracks = response.Items
                .Where(i => i.Track is FullTrack)
                .Select(i => (FullTrack)i.Track!)
                .ToList();
            return tracks;
        }
    }
}