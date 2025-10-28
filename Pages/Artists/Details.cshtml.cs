using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyAPI.Web;
using SpotifyProject.Services;

namespace SpotifyProject.Pages.Artists;

public class DetailsModel : PageModel
{
    private readonly SpotifyClientFactory _factory;
    private readonly ArtistMetricsService _metricsService;

    public FullArtist? Artist { get; private set; }
    public List<SimpleAlbum> Albums { get; private set; } = new();
    public List<FullTrack> TopTracks { get; private set; } = new();

    // Basic immediate metrics
    public BasicArtistMetrics? Metrics { get; private set; }

    public DetailsModel(SpotifyClientFactory factory, ArtistMetricsService metricsService)
    {
        _factory = factory;
        _metricsService = metricsService;
    }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var spotify = _factory.CreateAppClient();

        // Fetch artist and top tracks (display)
        Artist = await spotify.Artists.Get(id);
        var top = await spotify.Artists.GetTopTracks(id, new ArtistsTopTracksRequest("US"));
        TopTracks = top.Tracks?.ToList() ?? new List<FullTrack>();

        // Fetch a small first page of albums to populate the Albums list 
        var albumsPage = await spotify.Artists.GetAlbums(id, new ArtistsAlbumsRequest
        {
            IncludeGroupsParam = ArtistsAlbumsRequest.IncludeGroups.Album | ArtistsAlbumsRequest.IncludeGroups.Single,
            Limit = 20
        });
        Albums = albumsPage.Items?.ToList() ?? new List<SimpleAlbum>();

   
        Metrics = await _metricsService.GetBasicMetricsAsync(id);

        return Page();
    }
}