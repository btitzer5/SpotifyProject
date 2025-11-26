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
        private readonly GeminiService _geminiService;

        public ChatbotService(
            SpotifyService spotifyService,
            SpotifyClientFactory spotifyClientFactory,
            GeminiService geminiService)
        {
            _spotifyService = spotifyService;
            _spotifyClientFactory = spotifyClientFactory;
            _geminiService = geminiService;
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
                // Recently played / "what did I just listen to"
                else if (ContainsAny(lowerMessage,
                                     "recently played",
                                     "recent song",
                                     "what did i listen",
                                     "what did i just listen",
                                     "what did i listen to",
                                     "what did i just listen to",
                                     "what did i just listen to right now",
                                     "last played"))
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
                // Currently playing / "what song is playing right now"
                else if (ContainsAny(lowerMessage,
                                     "currently playing",
                                     "what's playing",
                                     "now playing",
                                     "what song is playing",
                                     "what song is playing right now",
                                     "what is playing right now"))
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
                // Help
                else if (ContainsAny(lowerMessage, "help", "what can you do", "commands"))
                {
                    return GetHelpMessage();
                }
                // Artist stats: followers / monthly listeners style questions
                else if (ContainsAny(lowerMessage, "followers", "monthly listeners", "monthly listener"))
                {
                    var artistName = ExtractArtistFromStatsQuestion(lowerMessage);
                    if (!string.IsNullOrWhiteSpace(artistName))
                    {
                        return await GetArtistStatsResponse(artistName, lowerMessage);
                    }

                    // If we cannot parse an artist name, fall back to Gemini
                    var aiStatsReply = await _geminiService.GetChatResponseAsync(message);
                    return aiStatsReply;
                }
                else
                {
                    // Fallback to Gemini for anything not handled by Spotify commands
                    var aiReply = await _geminiService.GetChatResponseAsync(message);
                    return aiReply;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("insufficient client scope", StringComparison.OrdinalIgnoreCase))
                {
                    return "I need extra Spotify permissions to do that. " +
                           "Please log out, log back in, and accept the requested Spotify permissions, then try again.";
                }

                return $"Sorry, I encountered an error: {ex.Message}";
            }
        }

        // ----------------- Spotify-specific handlers -----------------

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

        private async Task<string> GetArtistStatsResponse(string artistName, string originalQuery)
        {
            if (string.IsNullOrWhiteSpace(artistName))
            {
                return "Please tell me the artist name so I can look up their stats.";
            }

            var spotify = GetSpotifyClient();
            var searchRequest = new SearchRequest(SearchRequest.Types.Artist, artistName)
            {
                Limit = 1
            };
            var searchResult = await spotify.Search.Item(searchRequest);

            if (searchResult.Artists.Items.Count == 0)
            {
                return $"I could not find any artist named '{artistName}'.";
            }

            var artist = searchResult.Artists.Items[0];
            var genres = artist.Genres.Take(5);

            var sb = new StringBuilder();
            sb.AppendLine($"🎤 {artist.Name}");
            sb.AppendLine($"Followers: {artist.Followers.Total:N0}");
            sb.AppendLine($"Popularity: {artist.Popularity}/100");

            if (genres.Any())
            {
                sb.AppendLine($"Genres: {string.Join(", ", genres)}");
            }

            if (originalQuery.Contains("monthly listener"))
            {
                sb.AppendLine();
                sb.AppendLine("Spotify’s public API does not provide exact monthly listener counts.");
                sb.AppendLine("I can show followers and popularity, but not the precise monthly listeners.");
            }

            return sb.ToString();
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

📈 Stats:
• 'how many followers does [artist] have'
• 'how many monthly listeners does [artist] have'

⏰ Time Ranges (for top items):
Add 'short', 'medium', or 'long' to your query
• 'top artists short term' - Last 4 weeks
• 'top tracks medium term' - Last 6 months (default)
• 'top artists long term' - All time

Type any command to get started!";
        }

        // ----------------- Helpers -----------------

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

        private string ExtractArtistFromStatsQuestion(string message)
        {
            var text = message.Replace("?", "");

            var junkPhrases = new[]
            {
                "how many followers does",
                "how many followers do",
                "how many followers",
                "how many monthly listeners does",
                "how many monthly listeners do",
                "how many monthly listeners",
                "monthly listeners does",
                "monthly listeners do",
                "followers does",
                "followers do",
                "followers",
                "monthly listeners",
                "monthly listener",
                "have",
                "has",
                "does",
                "do",
                "the artist",
                "the band",
                "the group",
                "artist",
                "band",
                "group"
            };

            foreach (var phrase in junkPhrases)
            {
                text = text.Replace(phrase, "");
            }

            return text.Trim();
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
