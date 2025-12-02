using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using SpotifyProject;
using SpotifyProject.Models;
using SpotifyProject.Options;
using SpotifyProject.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

// Add controllers to the container.
builder.Services.AddControllersWithViews();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();

// Persist DataProtection keys to disk so session cookies can be unprotected across restarts
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys");
Directory.CreateDirectory(keysFolder);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("SpotifyProject");

// Configure session cookie options so OAuth redirects work with dev tunnels over HTTPS
builder.Services.AddSession(options =>
{
    // If you serve over HTTPS (recommended), set None + Secure
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.IdleTimeout = TimeSpan.FromHours(1);
});

// Configure forwarded headers so dev tunnels' X-Forwarded-* values are honored
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
    // trust all proxies (dev only). Clear KnownNetworks/KnownProxies so forwarded headers are accepted from any proxy.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Options binding
builder.Services.Configure<SpotifyOptions>(builder.Configuration.GetSection("Spotify"));

// NEW: bind Gemini options
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));

// Base config for Spotify client
builder.Services.AddSingleton(SpotifyClientConfig.CreateDefault());

// App services
builder.Services.AddScoped<SpotifyAuthService>();
builder.Services.AddScoped<SpotifyClientFactory>();
builder.Services.AddScoped<ISpotifySearchService, SpotifySearchService>();
builder.Services.AddScoped<SpotifyService>();

// NEW: Gemini service
builder.Services.AddSingleton<GeminiService>();

builder.Services.AddScoped<ChatbotService>();
builder.Services.AddSingleton<ArtistMetricsService>();

// Keep console logging available
builder.Logging.AddConsole();

// Read config/env now so we can log it after build
var spotifyOptions = builder.Configuration.GetSection("Spotify").Get<SpotifyOptions>();
var envOverride = Environment.GetEnvironmentVariable("Spotify__CallbackUrl");

var app = builder.Build();

// Apply forwarded headers early in pipeline so Request.Host/Scheme are correct
app.UseForwardedHeaders();

// Diagnostic logging: show configured callback value and any env override
app.Logger.LogInformation("Configured Spotify CallbackUrl from appsettings: {CallbackUrl}", spotifyOptions?.CallbackUrl);
app.Logger.LogInformation("Environment variable Spotify__CallbackUrl: {EnvCallback}", string.IsNullOrEmpty(envOverride) ? "(not set)" : envOverride);
app.Logger.LogInformation("Configured ClientId: {ClientId}", spotifyOptions?.ClientId);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// No HTTPS redirection in Development
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapRazorPages();
app.MapControllers();
app.UseAuthentication();  // Must come before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
