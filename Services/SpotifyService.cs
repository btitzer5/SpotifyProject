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
            return _spotifyClientFactory.CreateUserClient();
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
                // Let's try using a completely different approach - instantiate the request and use all possible property names
                var request = new PersonalizationTopRequest();
                request.Limit = limit;
                
                // Let's try setting every possible property that might exist
                var requestType = request.GetType();
                var properties = requestType.GetProperties();
                
                foreach (var prop in properties)
                {
                    // Try setting any property that might be related to time range
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
                                
                                // Try to find a matching enum value
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
                            // Ignore any errors setting individual properties
                        }
                    }
                }
                
                return await spotify.Personalization.GetTopTracks(request);
            }
            catch (Exception ex)
            {
                // Let's add a visible error message to see what's happening
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
                
                // Use the same property-setting logic
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
                            // Ignore errors
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
    }
}