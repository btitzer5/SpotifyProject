using SpotifyAPI.Web;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyProject.Models;

namespace SpotifyProject.Services
{
    public interface ISpotifySearchService
    {
        Task<SearchResultsViewModel> SearchAsync(AdvancedSearchRequest request);
        Task<List<string>> GetAvailableGenresAsync();
    }
}
