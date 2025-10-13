using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyAPI.Web;

namespace SpotifyProject.Pages.Artists;

public class DetailsModel : PageModel
{
    private readonly SpotifyClientFactory _factory;

    public FullArtist? Artist { get; private set; }
    public List<SimpleAlbum> Albums { get; private set; } = new();
    public List<FullTrack> TopTracks { get; private set; } = new();

    public DetailsModel(SpotifyClientFactory factory) => _factory = factory;

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var spotify = _factory.CreateAppClient();

        Artist = await spotify.Artists.Get(id);

        var albumsPage = await spotify.Artists.GetAlbums(
            id,
            new ArtistsAlbumsRequest
            {
                IncludeGroupsParam = ArtistsAlbumsRequest.IncludeGroups.Album | ArtistsAlbumsRequest.IncludeGroups.Single,
                Limit = 20
            });
        Albums = albumsPage.Items?.ToList() ?? new List<SimpleAlbum>();

        var top = await spotify.Artists.GetTopTracks(id, new ArtistsTopTracksRequest("US"));
        TopTracks = top.Tracks?.ToList() ?? new List<FullTrack>();

        return Page();
    }
}