namespace SpotifyProject;

public sealed class SpotifyOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string CallbackUrl { get; set; } = string.Empty;
}