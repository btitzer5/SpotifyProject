using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace SpotifyProject.Pages.Auth;

public class CallbackModel : PageModel
{
    private readonly SpotifyAuthService _auth;
    private readonly ILogger<CallbackModel> _logger;

    public CallbackModel(SpotifyAuthService auth, ILogger<CallbackModel> logger) => (_auth, _logger) = (auth, logger);

    public async Task<IActionResult> OnGetAsync(string? code, string? error, string? state)
    {
        if (!string.IsNullOrEmpty(error))
            return BadRequest(error);

        if (string.IsNullOrEmpty(code))
            return BadRequest("Missing authorization code.");

        try
        {
            _logger.LogInformation("Callback handler invoked. code present: {HasCode}, state: {State}", !string.IsNullOrEmpty(code), state);
        await _auth.StoreTokensFromCallbackAsync(code, state);
        return RedirectToPage("/Playlists");
    }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Spotify callback. state={State}", state);
            // Helpful dev-time response. Remove/replace in production.
            return StatusCode(500, $"Auth error: {ex.Message}");
        }
    }
}