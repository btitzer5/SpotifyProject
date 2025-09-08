using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;

namespace SpotifyProject;

public sealed class SpotifyAuthService
{
    private readonly IOptions<SpotifyOptions> _options;
    private readonly IHttpContextAccessor _http;
    private readonly SpotifyClientConfig _baseConfig;
    private readonly IDistributedCache _cache;

    public SpotifyAuthService(IOptions<SpotifyOptions> options,
                              IHttpContextAccessor http,
                              SpotifyClientConfig baseConfig,
                              IDistributedCache cache)
    {
        _options = options;
        _http = http;
        _baseConfig = baseConfig;
        _cache = cache;
    }

    public Uri BuildLoginUri(string[] scopes)
    {
        var (verifier, challenge) = PKCEUtil.GenerateCodes();

        // Store verifier server-side keyed by a random state (survives cross-host redirects)
        var state = Guid.NewGuid().ToString("N");
        _cache.SetString($"pkce:{state}", verifier, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        var request = new LoginRequest(
            new Uri(_options.Value.CallbackUrl),
            _options.Value.ClientId,
            LoginRequest.ResponseType.Code)
        {
            CodeChallenge = challenge,
            CodeChallengeMethod = "S256",
            Scope = scopes,
            State = state
        };

        return request.ToUri();
    }

    public async Task StoreTokensFromCallbackAsync(string code, string? state, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(state))
            throw new InvalidOperationException("Missing OAuth state.");

        var verifier = await _cache.GetStringAsync($"pkce:{state}", ct);
        if (string.IsNullOrEmpty(verifier))
            throw new InvalidOperationException("Missing PKCE verifier. Please restart login.");

        var oAuth = new OAuthClient(_baseConfig);
        var tokenResponse = await oAuth.RequestToken(
            new PKCETokenRequest(_options.Value.ClientId, code, new Uri(_options.Value.CallbackUrl), verifier),
            ct);

        await _cache.RemoveAsync($"pkce:{state}", ct);

        var json = JsonSerializer.Serialize(tokenResponse);
        _http.HttpContext!.Session.SetString(Constants.SessionTokensKey, json);
    }
}