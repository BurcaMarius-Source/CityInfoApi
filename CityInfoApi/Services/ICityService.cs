using CityInfoApi.DTOs;
using CityInfoApi.Models;

namespace CityInfoApi.Services
{
    public interface ICityService
    {
        Task<City?> AddCityAsync(AddCityRequest request);
        Task<bool> UpdateCityAsync(int id, int touristRating, DateTime dateEstablished, long estimatedPopulation);
        Task<bool> DeleteCityAsync(int id);
        Task<IEnumerable<CitySearchResultDto>> SearchCityByNameAsync(string name);
    }
}
