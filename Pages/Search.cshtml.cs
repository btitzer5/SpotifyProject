using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyAPI.Web;

namespace SpotifyProject.Pages;

public class SearchModel : PageModel
{
    private readonly SpotifyClientFactory _factory;
    public List<FullTrack> Tracks { get; private set; } = new();

    public SearchModel(SpotifyClientFactory factory) => _factory = factory;

    public async Task OnGetAsync(string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return;

        var spotify = _factory.CreateAppClient();
        var result = await spotify.Search.Item(new SearchRequest(SearchRequest.Types.Track, q));
        Tracks = result.Tracks?.Items ?? new List<FullTrack>();
    }
}