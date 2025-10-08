using CityInfoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CityInfoApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

        public DbSet<City> Cities { get; set; } = null!;
    }
}
