using CityInfoApi.Data;
using CityInfoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CityInfoApi.Repositories
{
    public class CityRepository : ICityRepository
    {
        private readonly AppDbContext _db;
        public CityRepository(AppDbContext db) => _db = db;

        public async Task<City?> AddAsync(AddCityRequest request)
        {
            bool exists = await _db.Cities.AnyAsync(c =>
                c.Name.ToLower() == request.Name.ToLower() &&
                c.State.ToLower() == request.State.ToLower());

            if (exists)
            {
                return null;
            }

            var newCity = new City()
            {
                Country = request.Country,
                EstimatedPopulation = request.EstimatedPopulation,
                DateEstablished = request.DateEstablished,
                Name = request.Name,
                State = request.State,
                TouristRating = request.TouristRating,
            };
            _db.Cities.Add(newCity);
            await _db.SaveChangesAsync();

            return newCity;
        }

        public async Task<City?> GetByIdAsync(int id) =>
            await _db.Cities.FindAsync(id);

        public async Task<IEnumerable<City>> SearchByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Enumerable.Empty<City>();

            name = name.Trim();
            return await _db.Cities
                .Where(c => EF.Functions.Like(c.Name, $"%{name}%"))
                .ToListAsync();
        }

        public async Task UpdateAsync(City city)
        {
            _db.Entry(city).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var c = await _db.Cities.FindAsync(id);
            if (c == null) return;
            _db.Cities.Remove(c);
            await _db.SaveChangesAsync();
        }
    }
}
