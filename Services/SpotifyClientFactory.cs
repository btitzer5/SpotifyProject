using System;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;

namespace SpotifyProject;

public sealed class SpotifyClientFactory
{
    private readonly IHttpContextAccessor _http;
    private readonly IOptions<SpotifyOptions> _options;
    private readonly SpotifyClientConfig _baseConfig;

    public SpotifyClientFactory(IHttpContextAccessor http,
                                IOptions<SpotifyOptions> options,
                                SpotifyClientConfig baseConfig)
    {
        _http = http;
        _options = options;
        _baseConfig = baseConfig;
    }

    // App-only client (no user), good for search and public metadata
    public SpotifyClient CreateAppClient()
    {
        if (string.IsNullOrWhiteSpace(_options.Value.ClientSecret))
            throw new InvalidOperationException("ClientSecret is required for Client Credentials flow.");

        var authenticator = new ClientCredentialsAuthenticator(_options.Value.ClientId, _options.Value.ClientSecret);
        return new SpotifyClient(_baseConfig.WithAuthenticator(authenticator));
    }

    // User-authenticated client (needed for current user’s playlists, library, etc.)
    public SpotifyClient CreateUserClient()
    {
        var json = _http.HttpContext!.Session.GetString(Constants.SessionTokensKey);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("User is not authenticated with Spotify.");

        var tokens = JsonSerializer.Deserialize<PKCETokenResponse>(json)!;

        var authenticator = new PKCEAuthenticator(_options.Value.ClientId, tokens);
        authenticator.TokenRefreshed += (_, newTokens) =>
        {
            _http.HttpContext!.Session.SetString(Constants.SessionTokensKey, JsonSerializer.Serialize(newTokens));
        };

        return new SpotifyClient(_baseConfig.WithAuthenticator(authenticator));
    }
}