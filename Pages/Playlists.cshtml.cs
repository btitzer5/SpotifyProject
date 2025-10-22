using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SpotifyProject.Pages;

public class PlaylistsModel : PageModel
{
    private readonly SpotifyClientFactory _factory;
    private readonly ILogger<PlaylistsModel> _logger;

    public List<FullPlaylist> Playlists { get; private set; } = new();

    public PlaylistsModel(SpotifyClientFactory factory, ILogger<PlaylistsModel> logger) => (_factory, _logger) = (factory, logger);

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var spotify = _factory.CreateUserClient();
            var page = await spotify.Playlists.CurrentUsers();
            Playlists = page.Items.ToList();
            return Page();
        }
        catch (InvalidOperationException)
        {
            // Not authenticated (no session tokens)
            return RedirectToPage("/Auth/Login");
        }
        catch (APIException apiEx)
        {
            // Log detailed API info for diagnosis
            _logger.LogError(apiEx, "Spotify API error. Message: {Message}", apiEx.Message);

            // Try to extract status code if available
            if (apiEx.Response != null)
            {
                _logger.LogError("Spotify API response status: {Status}", apiEx.Response.StatusCode);
            }

            // If unauthorized, send user to login
            if (apiEx.Response != null && apiEx.Response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return RedirectToPage("/Auth/Login");
            }

            // Show friendly message / status code to developer
            return StatusCode(502, $"Spotify API error: {apiEx.Message}");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving playlists.");
            return StatusCode(500, "Unexpected error.");
        }
    }
}