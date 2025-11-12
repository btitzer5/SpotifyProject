using SpotifyAPI.Web;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyProject.Services
{
    public class ChatbotService
    {
        private readonly SpotifyService _spotifyService;
        private readonly SpotifyClientFactory _spotifyClientFactory;

        public ChatbotService(SpotifyService spotifyService, SpotifyClientFactory spotifyClientFactory)
        {
            _spotifyService = spotifyService;
            _spotifyClientFactory = spotifyClientFactory;
        }

        private SpotifyClient GetSpotifyClient()
        {
            return _spotifyClientFactory.CreateUserClient();
        }

        public async Task<string> ProcessMessage(string message)
        {
            var lowerMessage = message.ToLower().Trim();

            try
            {
                // User's personal data queries
                if (ContainsAny(lowerMessage, "top artist", "favorite artist", "my artist"))
                {
                    return await GetTopArtistsResponse(lowerMessage);
                }
                else if (ContainsAny(lowerMessage, "top track", "top song", "favorite song", "favorite track"))
                {
                    return await GetTopTracksResponse(lowerMessage);
                }
                else if (ContainsAny(lowerMessage, "recently played", "recent song", "what did i listen", "last played"))
                {
                    return await GetRecentlyPlayedResponse();
                }
                else if (ContainsAny(lowerMessage, "profile", "who am i", "my name", "my account"))
                {
                    return await GetUserProfileResponse();
                }
                else if (ContainsAny(lowerMessage, "playlist", "my playlist"))
                {
                    return await GetPlaylistsResponse();
                }
                else if (ContainsAny(lowerMessage, "currently playing", "what's playing", "now playing"))
                {
                    return await GetCurrentlyPlayingResponse();
                }
                else if (ContainsAny(lowerMessage, "saved track", "liked song", "my song"))
                {
                    return await GetSavedTracksResponse();
                }
                // Artist search queries
                else if (lowerMessage.StartsWith("artist ") || lowerMessage.StartsWith("tell me about ") ||
                         lowerMessage.StartsWith("who is ") || lowerMessage.StartsWith("search "))
                {
                    var artistName = ExtractArtistName(lowerMessage);
                    return await SearchArtistResponse(artistName);
                }
                else if (ContainsAny(lowerMessage, "search for", "find artist", "look up"))
                {
                    var artistName = ExtractSearchQuery(lowerMessage);
                    return await SearchArtistResponse(artistName);
                }
                // Album queries
                else if (ContainsAny(lowerMessage, "album by", "albums by"))
                {
                    var artistName = lowerMessage.Replace("album by", "").Replace("albums by", "").Trim();
                    return await GetArtistAlbumsResponse(artistName);
                }
                // Track queries
                else if (lowerMessage.StartsWith("track ") || lowerMessage.StartsWith("song "))
                {
                    var trackName = lowerMessage.Replace("track ", "").Replace("song ", "").Trim();
                    return await SearchTrackResponse(trackName);
                }
                else if (ContainsAny(lowerMessage, "help", "what can you do", "commands"))
                {
                    return GetHelpMessage();
                }
                else
                {
                    return "I'm not sure what you're asking. Type 'help' to see what I can do!";
                }
            }
            catch (Exception ex)
            {
                return $"Sorry, I encountered an error: {ex.Message}";
            }
        }

        private async Task<string> GetTopArtistsResponse(string query)
        {
            var timeRange = ExtractTimeRange(query);
            var limit = ExtractLimit(query, 5);

            var artists = await _spotifyService.GetUserTopArtists(limit, timeRange);

            var response = new StringBuilder();
            response.AppendLine($"🎤 Your Top {limit} Artists ({GetTimeRangeText(timeRange)}):\n");

            int i = 1;
            foreach (var artist in artists.Items)
            {
                var genres = artist.Genres.Take(2);
                var genreText = genres.Any() ? $" ({string.Join(", ", genres)})" : "";
                response.AppendLine($"{i}. {artist.Name}{genreText}");
                response.AppendLine($"   Popularity: {artist.Popularity}/100 | Followers: {artist.Followers.Total:N0}");
                i++;
            }

            return response.ToString();
        }

        private async Task<string> GetTopTracksResponse(string query)
        {
            var timeRange = ExtractTimeRange(query);
            var limit = ExtractLimit(query, 5);

            var tracks = await _spotifyService.GetUserTopTracks(limit, timeRange);

            var response = new StringBuilder();
            response.AppendLine($"🎵 Your Top {limit} Tracks ({GetTimeRangeText(timeRange)}):\n");

            int i = 1;
            foreach (var track in tracks.Items)
            {
                var artists = string.Join(", ", track.Artists.Select(a => a.Name));
                response.AppendLine($"{i}. {track.Name}");
                response.AppendLine($"   by {artists}");
                response.AppendLine($"   Album: {track.Album.Name}");
                i++;
            }

            return response.ToString();
        }

        private async Task<string> GetRecentlyPlayedResponse()
        {
            var spotify = GetSpotifyClient();
            var recent = await spotify.Player.GetRecentlyPlayed(new PlayerRecentlyPlayedRequest { Limit = 10 });

            var response = new StringBuilder();
            response.AppendLine("🕐 Your Recently Played Tracks:\n");

            foreach (var item in recent.Items)
            {
                var artists = string.Join(", ", item.Track.Artists.Select(a => a.Name));
                var playedAt = item.PlayedAt.ToLocalTime();
                response.AppendLine($"• {item.Track.Name} by {artists}");
                response.AppendLine($"  Played at: {playedAt:g}");
            }

            return response.ToString();
        }

        private async Task<string> GetUserProfileResponse()
        {
            var profile = await _spotifyService.GetCurrentUserProfile();

            var response = new StringBuilder();
            response.AppendLine("👤 Your Spotify Profile:\n");
            response.AppendLine($"Name: {profile.DisplayName}");
            response.AppendLine($"Email: {profile.Email}");
            response.AppendLine($"Country: {profile.Country}");
            response.AppendLine($"Followers: {profile.Followers.Total:N0}");
            response.AppendLine($"Product: {profile.Product}");

            return response.ToString();
        }

        private async Task<string> GetPlaylistsResponse()
        {
            var spotify = GetSpotifyClient();
            var playlists = await spotify.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest { Limit = 10 });

            var response = new StringBuilder();
            response.AppendLine("📋 Your Playlists:\n");

            foreach (var playlist in playlists.Items)
            {
                response.AppendLine($"• {playlist.Name}");
                response.AppendLine($"  {playlist.Tracks.Total} tracks | {(playlist.Public ?? false ? "Public" : "Private")}");
            }

            return response.ToString();
        }

        private async Task<string> GetCurrentlyPlayingResponse()
        {
            var spotify = GetSpotifyClient();
            var currentlyPlaying = await spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());

            if (currentlyPlaying?.Item is FullTrack track)
            {
                var artists = string.Join(", ", track.Artists.Select(a => a.Name));
                var progress = TimeSpan.FromMilliseconds(currentlyPlaying.ProgressMs ?? 0);
                var duration = TimeSpan.FromMilliseconds(track.DurationMs);

                return $"🎧 Currently Playing:\n\n" +
                       $"{track.Name}\n" +
                       $"by {artists}\n" +
                       $"Album: {track.Album.Name}\n" +
                       $"Progress: {progress:mm\\:ss} / {duration:mm\\:ss}\n" +
                       $"Playing: {(currentlyPlaying.IsPlaying ? "▶️ Yes" : "⏸️ Paused")}";
            }

            return "No track is currently playing.";
        }

        private async Task<string> GetSavedTracksResponse()
        {
            var spotify = GetSpotifyClient();
            var savedTracks = await spotify.Library.GetTracks(new LibraryTracksRequest { Limit = 10 });

            var response = new StringBuilder();
            response.AppendLine("💚 Your Saved Tracks (Recent 10):\n");

            foreach (var item in savedTracks.Items)
            {
                var artists = string.Join(", ", item.Track.Artists.Select(a => a.Name));
                response.AppendLine($"• {item.Track.Name} by {artists}");
            }

            return response.ToString();
        }

        private async Task<string> SearchArtistResponse(string artistName)
        {
            if (string.IsNullOrWhiteSpace(artistName))
            {
                return "Please provide an artist name to search for.";
            }

            var spotify = GetSpotifyClient();
            var searchRequest = new SearchRequest(SearchRequest.Types.Artist, artistName) { Limit = 1 };
            var searchResult = await spotify.Search.Item(searchRequest);

            if (searchResult.Artists.Items.Count == 0)
            {
                return $"I couldn't find any artist named '{artistName}'.";
            }

            var artist = searchResult.Artists.Items[0];
            var genres = artist.Genres.Take(5);

            var response = new StringBuilder();
            response.AppendLine($"🎤 Artist: {artist.Name}\n");
            response.AppendLine($"Followers: {artist.Followers.Total:N0}");
            response.AppendLine($"Popularity: {artist.Popularity}/100");

            if (genres.Any())
            {
                response.AppendLine($"Genres: {string.Join(", ", genres)}");
            }

            if (!string.IsNullOrEmpty(artist.Uri))
            {
                response.AppendLine($"\nSpotify URI: {artist.Uri}");
            }

            return response.ToString();
        }

        private async Task<string> GetArtistAlbumsResponse(string artistName)
        {
            if (string.IsNullOrWhiteSpace(artistName))
            {
                return "Please provide an artist name.";
            }

            var spotify = GetSpotifyClient();
            var searchRequest = new SearchRequest(SearchRequest.Types.Artist, artistName) { Limit = 1 };
            var searchResult = await spotify.Search.Item(searchRequest);

            if (searchResult.Artists.Items.Count == 0)
            {
                return $"I couldn't find any artist named '{artistName}'.";
            }

            var artist = searchResult.Artists.Items[0];
            var albums = await spotify.Artists.GetAlbums(artist.Id, new ArtistsAlbumsRequest { Limit = 10 });

            var response = new StringBuilder();
            response.AppendLine($"💿 Albums by {artist.Name}:\n");

            foreach (var album in albums.Items)
            {
                response.AppendLine($"• {album.Name} ({album.ReleaseDate})");
                response.AppendLine($"  {album.TotalTracks} tracks | Type: {album.AlbumType}");
            }

            return response.ToString();
        }

        private async Task<string> SearchTrackResponse(string trackName)
        {
            if (string.IsNullOrWhiteSpace(trackName))
            {
                return "Please provide a track name to search for.";
            }

            var spotify = GetSpotifyClient();
            var searchRequest = new SearchRequest(SearchRequest.Types.Track, trackName) { Limit = 5 };
            var searchResult = await spotify.Search.Item(searchRequest);

            if (searchResult.Tracks.Items.Count == 0)
            {
                return $"I couldn't find any track named '{trackName}'.";
            }

            var response = new StringBuilder();
            response.AppendLine($"🎵 Search Results for '{trackName}':\n");

            int i = 1;
            foreach (var track in searchResult.Tracks.Items)
            {
                var artists = string.Join(", ", track.Artists.Select(a => a.Name));
                var duration = TimeSpan.FromMilliseconds(track.DurationMs);

                response.AppendLine($"{i}. {track.Name}");
                response.AppendLine($"   by {artists}");
                response.AppendLine($"   Album: {track.Album.Name} | Duration: {duration:mm\\:ss}");
                i++;
            }

            return response.ToString();
        }

        private string GetHelpMessage()
        {
            return @"🤖 Spotify Chatbot Commands:

📊 Your Data:
• 'top artists' - Show your favorite artists
• 'top tracks' - Show your favorite songs
• 'recently played' - Show recently played tracks
• 'profile' - Show your profile info
• 'playlists' - Show your playlists
• 'currently playing' - What's playing now
• 'saved tracks' - Show your liked songs

🔍 Search:
• 'artist [name]' - Get info about an artist
• 'albums by [artist]' - Show albums by an artist
• 'track [name]' - Search for a track

⏰ Time Ranges (for top items):
Add 'short', 'medium', or 'long' to your query
• 'top artists short term' - Last 4 weeks
• 'top tracks medium term' - Last 6 months (default)
• 'top artists long term' - All time

Type any command to get started!";
        }

        // Helper methods
        private bool ContainsAny(string text, params string[] keywords)
        {
            return keywords.Any(keyword => text.Contains(keyword));
        }

        private string ExtractArtistName(string message)
        {
            return message
                .Replace("artist ", "")
                .Replace("tell me about ", "")
                .Replace("who is ", "")
                .Replace("search ", "")
                .Trim();
        }

        private string ExtractSearchQuery(string message)
        {
            return message
                .Replace("search for ", "")
                .Replace("find artist ", "")
                .Replace("look up ", "")
                .Trim();
        }

        private string ExtractTimeRange(string query)
        {
            if (query.Contains("short"))
                return "short_term";
            else if (query.Contains("long"))
                return "long_term";
            else
                return "medium_term";
        }

        private int ExtractLimit(string query, int defaultLimit)
        {
            var words = query.Split(' ');
            foreach (var word in words)
            {
                if (int.TryParse(word, out int limit) && limit > 0 && limit <= 50)
                {
                    return limit;
                }
            }
            return defaultLimit;
        }

        private string GetTimeRangeText(string timeRange)
        {
            return timeRange switch
            {
                "short_term" => "Last 4 weeks",
                "medium_term" => "Last 6 months",
                "long_term" => "All time",
                _ => "Medium term"
            };
        }
    }
}