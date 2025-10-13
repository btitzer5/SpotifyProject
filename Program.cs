using SpotifyAPI.Web;
using SpotifyProject;
using SpotifyProject.Models;
using SpotifyProject.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Options binding
builder.Services.Configure<SpotifyOptions>(builder.Configuration.GetSection("Spotify"));

// Base config for Spotify client
builder.Services.AddSingleton(SpotifyClientConfig.CreateDefault());

// App services
builder.Services.AddScoped<SpotifyAuthService>();
builder.Services.AddScoped<SpotifyClientFactory>();
builder.Services.AddScoped<ISpotifySearchService, SpotifySearchService>();

var app = builder.Build();

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
