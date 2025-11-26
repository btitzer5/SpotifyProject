using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpotifyProject.Models
{
    public class AdvancedSearchRequest
    {
        public string? Query { get; set; }
        public SearchType SearchType { get; set; } = SearchType.All;
        public string? Genre { get; set; }
        public int? Year { get; set; }
        public int? FromYear { get; set; }
        public int? ToYear { get; set; }
        public string? Artist { get; set; }
        public string? Album { get; set; }
        public string? Track { get; set; }
        public bool IsExplicit { get; set; }
        public MarketFilter Market { get; set; } = MarketFilter.Any;
        public PopularityFilter Popularity { get; set; } = PopularityFilter.Any;
        public int Limit { get; set; } = 20;
    }

    public enum SearchType
    {
        All,
        Artists,
        Tracks,
        Albums,
        Playlists
    }

    public enum MarketFilter
    {
        Any,
        US,
        GB,
        CA,
        AU,
        DE,
        FR,
        ES,
        IT,
        JP
    }

    public enum PopularityFilter
    {
        Any,
        Low,       // 0-33
        Medium,    // 34-66
        High       // 67-100
    }

    public class SearchResultsViewModel
    {
        public AdvancedSearchRequest SearchRequest { get; set; } = new();
        public List<FullArtist> Artists { get; set; } = new();
        public List<FullTrack> Tracks { get; set; } = new();
        public List<SimpleAlbum> Albums { get; set; } = new();
        public List<FullPlaylist> Playlists { get; set; } = new();
        public bool HasResults => Artists.Any() || Tracks.Any() || Albums.Any() || Playlists.Any();
        public string? ErrorMessage { get; set; }
    }
}
