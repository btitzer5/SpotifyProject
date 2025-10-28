using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SpotifyAPI.Web;

namespace SpotifyProject.Services;

public sealed record BasicArtistMetrics(
    int Followers,
    int Popularity,
    int TopTracksCount,
    int GenreCount,
    string? PrimaryImageUrl,
    DateTimeOffset RetrievedAt
);

public class ArtistMetricsService
{
    private readonly IServiceProvider _provider;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);

    public ArtistMetricsService(IServiceProvider provider, IMemoryCache cache)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<BasicArtistMetrics> GetBasicMetricsAsync(string artistId)
    {
        if (string.IsNullOrWhiteSpace(artistId)) throw new ArgumentNullException(nameof(artistId));

        if (_cache.TryGetValue<BasicArtistMetrics>(artistId, out var cached))
            return cached;

        using var scope = _provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<SpotifyClientFactory>();
        var spotify = factory.CreateAppClient();

            // Fetch artist and top tracks concurrently (fast)
        var artistTask = ExecuteWithRetryAsync(() => spotify.Artists.Get(artistId));
        var topTracksTask = ExecuteWithRetryAsync(() => spotify.Artists.GetTopTracks(artistId, new ArtistsTopTracksRequest("US")));

        await Task.WhenAll(artistTask, topTracksTask);

        var artist = artistTask.Result;
        var top = topTracksTask.Result;

        var metrics = new BasicArtistMetrics(
            Followers: artist.Followers?.Total ?? 0,
            Popularity: artist.Popularity,
            TopTracksCount: top?.Tracks?.Count ?? 0,
            GenreCount: artist.Genres?.Count ?? 0,
            PrimaryImageUrl: artist.Images?.OrderByDescending(i => i.Width).FirstOrDefault()?.Url,
            RetrievedAt: DateTimeOffset.UtcNow
        );

        _cache.Set(artistId, metrics, _cacheTtl);
        return metrics;
    }

    // Simple retry for transient API errors (keeps calls reliable)
    private static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3, int initialDelayMs = 500)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (APIException) when (attempt < maxRetries)
            {
                var delay = initialDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay);
                continue;
            }
        }
    }
}
