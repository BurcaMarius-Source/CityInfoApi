using CityInfoApi.Models;

namespace CityInfoApi.Repositories
{
    public interface ICityRepository
    {
        Task<City?> AddAsync(AddCityRequest request);
        Task<City?> GetByIdAsync(int id);
        Task<IEnumerable<City>> SearchByNameAsync(string name);
        Task UpdateAsync(City city);
        Task DeleteAsync(int id);
    }
}
