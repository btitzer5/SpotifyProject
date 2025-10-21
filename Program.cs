using SpotifyAPI.Web;
using SpotifyProject;
using SpotifyProject.Models;
using SpotifyProject.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

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

// Base config for Spotify client
builder.Services.AddSingleton(SpotifyClientConfig.CreateDefault());

// App services
builder.Services.AddScoped<SpotifyAuthService>();
builder.Services.AddScoped<SpotifyClientFactory>();
builder.Services.AddScoped<ISpotifySearchService, SpotifySearchService>();
builder.Services.AddScoped<SpotifyService>();

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

app.Run();
