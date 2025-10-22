using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;

namespace SpotifyProject;

public sealed class SpotifyAuthService
{
    private readonly IOptions<SpotifyOptions> _options;
    private readonly IHttpContextAccessor _http;
    private readonly SpotifyClientConfig _baseConfig;
    private readonly IDistributedCache _cache;
    private readonly ILogger<SpotifyAuthService> _logger;

    public SpotifyAuthService(IOptions<SpotifyOptions> options,
                              IHttpContextAccessor http,
                              SpotifyClientConfig baseConfig,
                              IDistributedCache cache,
                              ILogger<SpotifyAuthService> logger)
    {
        _options = options;
        _http = http;
        _baseConfig = baseConfig;
        _cache = cache;
        _logger = logger;
    }

    private Uri GetCallbackUri()
    {
        var req = _http.HttpContext?.Request;
        if (req != null && req.Host.HasValue)
        {
            var scheme = req.Scheme;
            var host = req.Host.Value;
            var callback = new Uri($"{scheme}://{host}/auth/callback");
            _logger.LogInformation("Computed callback from request: Scheme={Scheme}, Host={Host}, Callback={Callback}", scheme, host, callback);
            return callback;
        }

        _logger.LogInformation("Falling back to configured Spotify CallbackUrl: {Configured}", _options.Value.CallbackUrl);
        return new Uri(_options.Value.CallbackUrl);
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

        // Diagnostic: log the state (do NOT log the verifier value)
        _logger.LogInformation("PKCE stored. State={State}", state);

        var callback = GetCallbackUri();

        var request = new LoginRequest(
            callback,
            _options.Value.ClientId,
            LoginRequest.ResponseType.Code)
        {
            CodeChallenge = challenge,
            CodeChallengeMethod = "S256",
            Scope = scopes,
            State = state
        };

        var uri = request.ToUri();
        _logger.LogInformation("Login URI generated: {Uri}", uri);
        return uri;
    }

    public async Task StoreTokensFromCallbackAsync(string code, string? state, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(state))
            throw new InvalidOperationException("Missing OAuth state.");

        // Diagnostic: log the incoming state value from Spotify
        _logger.LogInformation("Callback received. State={State}", state);

        var verifier = await _cache.GetStringAsync($"pkce:{state}", ct);
        if (string.IsNullOrEmpty(verifier))
        {
            _logger.LogWarning("PKCE verifier missing for state {State}. Cache keys not found or different host/process handled callback.", state);
            throw new InvalidOperationException("Missing PKCE verifier. Please restart login.");
        }

        var callback = GetCallbackUri();

        var oAuth = new OAuthClient(_baseConfig);
        var tokenResponse = await oAuth.RequestToken(
            new PKCETokenRequest(_options.Value.ClientId, code, callback, verifier),
            ct);

        await _cache.RemoveAsync($"pkce:{state}", ct);

        var json = JsonSerializer.Serialize(tokenResponse);
        _http.HttpContext!.Session.SetString(Constants.SessionTokensKey, json);

        _logger.LogInformation("Token exchange successful. Scopes: {Scopes}, ExpiresIn: {ExpiresIn}", tokenResponse.Scope, tokenResponse.ExpiresIn);
    }
}