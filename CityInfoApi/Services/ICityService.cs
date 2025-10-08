using CityInfoApi.DTOs;
using CityInfoApi.Models;

namespace CityInfoApi.Services
{
    public interface ICityService
    {
        Task<Result<City>> AddCityAsync(AddCityRequest request);
        Task<Result<City>> UpdateCityAsync(int id, int touristRating, DateTime dateEstablished, long estimatedPopulation);
        Task<Result<City>> DeleteCityAsync(int id);
        Task<Result<IEnumerable<CitySearchResultDto>>> SearchCityByNameAsync(string name);
    }
}
