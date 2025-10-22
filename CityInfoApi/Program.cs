
using CityInfoApi.Data;
using CityInfoApi.Repositories;
using CityInfoApi.Services;
using CityInfoApi.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

namespace CityInfoApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            //builder.Services.AddDbContext<AppDbContext>(options =>
            //    options.UseInMemoryDatabase("CitiesDb"));

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("CitiesDb")));

            builder.Services.AddScoped<ICityRepository, CityRepository>();
            builder.Services.AddScoped<ICityService, CityService>();

            builder.Services.AddHttpClient("RestCountries", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["RestCountries:BaseUrl"] ?? "https://restcountries.com/v3.1/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            // VisualCrossing client
            builder.Services.AddHttpClient("VisualCrossing", client =>
            {
                client.BaseAddress = new Uri("https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            builder.Services.AddControllers();
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddFluentValidationClientsideAdapters();
            builder.Services.AddValidatorsFromAssemblyContaining<AddCityRequestValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UpdateCityRequestValidator>();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddMemoryCache();

            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
