using Microsoft.Extensions.Options;
using SpotifyProject.Options;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpotifyProject.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;

        public GeminiService(IHttpClientFactory httpClientFactory, IOptions<GeminiOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient();
            _options = options.Value;
        }

        public async Task<string> GetChatResponseAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return "Gemini is not configured. Please set the API key in appsettings.json.";
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text =
                                    "You are an assistant inside a Spotify web app. " +
                                    "You do NOT have direct access to the user's live Spotify player " +
                                    "or their listening history, unless the app explicitly shows it to you. " +
                                    "If the user asks 'what did I just listen to' or 'what is playing right now', " +
                                    "explain that you cannot see their current player state and suggest they use " +
                                    "the built-in Spotify commands like 'recently played' or 'currently playing'. " +
                                    "Never invent fake placeholders like [Song Title] or [Artist Name]; always be honest " +
                                    "about what you know. Answer clearly and concisely.\n\n" +
                                    $"User message: {userMessage}"
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _options.ApiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Gemini error ({(int)response.StatusCode}): {responseContent}";
            }

            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                var text =
                    root.GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                return string.IsNullOrWhiteSpace(text)
                    ? "Gemini returned an empty response."
                    : text.Trim();
            }
            catch (Exception ex)
            {
                return $"Failed to parse Gemini response: {ex.Message}";
            }
        }
    }
}
