using CityInfoApi.Common;
using CityInfoApi.DTOs;
using CityInfoApi.Models;
using CityInfoApi.Repositories;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Text.Json;

namespace CityInfoApi.Services
{
    public class CityService(
        ICityRepository repo,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<CityService> logger,
        IMemoryCache cache) : ICityService
    {
        private readonly ICityRepository _repo = repo;
        private readonly IHttpClientFactory _httpFactory = httpFactory;
        private readonly IConfiguration _config = config;
        private readonly ILogger<CityService> _logger = logger;
        private readonly IMemoryCache _cache = cache;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        // -----------------------------
        // ADD CITY
        // -----------------------------
        public async Task<Result<City>> AddCityAsync(AddCityRequest request)
        {
            AppLogger.LogActionStart(_logger, ApiActions.AddCity, request);

            var addedCity = await _repo.AddAsync(request);
            if (addedCity == null)
            {
                AppLogger.LogWarning(_logger, ApiMessages.DuplicateCity, new { request.Name, request.Country });
                return Result<City>.Conflict(ApiMessages.DuplicateCity);
            }

            AppLogger.LogActionSuccess(_logger, ApiActions.AddCity, new { addedCity.Id, addedCity.Name });
            return Result<City>.Created(addedCity, ApiMessages.CityCreated);
        }

        // -----------------------------
        // UPDATE CITY
        // -----------------------------
        public async Task<Result<City>> UpdateCityAsync(
            int id,
            int touristRating,
            DateTime dateEstablished,
            long estimatedPopulation)
        {
            AppLogger.LogActionStart(_logger, ApiActions.UpdateCity, new { id });

            var city = await _repo.GetByIdAsync(id);
            if (city == null)
            {
                AppLogger.LogWarning(_logger, ApiMessages.CityNotFound, new { id });
                return Result<City>.NotFound(ApiMessages.CityNotFound);
            }

            city.TouristRating = touristRating;
            city.DateEstablished = dateEstablished;
            city.EstimatedPopulation = estimatedPopulation;

            await _repo.UpdateAsync(city);
            AppLogger.LogActionSuccess(_logger, ApiActions.UpdateCity, new { id });

            return Result<City>.NoContent(ApiMessages.CityUpdated);
        }

        // -----------------------------
        // DELETE CITY
        // -----------------------------
        public async Task<Result<City>> DeleteCityAsync(int id)
        {
            AppLogger.LogActionStart(_logger, ApiActions.DeleteCity, new { id });

            var city = await _repo.GetByIdAsync(id);
            if (city == null)
            {
                AppLogger.LogWarning(_logger, ApiMessages.CityNotFound, new { id });
                return Result<City>.NotFound(ApiMessages.CityNotFound);
            }

            await _repo.DeleteAsync(id);
            AppLogger.LogActionSuccess(_logger, ApiActions.DeleteCity, new { id });

            return Result<City>.NoContent(ApiMessages.CityDeleted);
        }

        // -----------------------------
        // SEARCH CITY
        // -----------------------------
        public async Task<Result<IEnumerable<CitySearchResultDto>>> SearchCityByNameAsync(string name)
        {
            AppLogger.LogActionStart(_logger, ApiActions.SearchCity, new { name });

            var cities = (await _repo.SearchByNameAsync(name)).ToList();
            var countryCache = new Dictionary<string, CountryInfo?>(StringComparer.OrdinalIgnoreCase);

            if (!cities.Any())
            {
                AppLogger.LogWarning(_logger, ApiMessages.NoResults, new { name });
                return Result<IEnumerable<CitySearchResultDto>>.NotFound(ApiMessages.NoResults);
            }

            var results = new List<CitySearchResultDto>();

            foreach (var c in cities)
            {
                var dto = new CitySearchResultDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    State = c.State,
                    Country = c.Country,
                    TouristRating = c.TouristRating,
                    DateEstablished = c.DateEstablished,
                    EstimatedPopulation = c.EstimatedPopulation
                };

                // Country info
                try
                {
                    if (!countryCache.TryGetValue(c.Country, out var countryInfo))
                    {
                        countryInfo = await GetCachedCountryInfoAsync(c.Country);
                        countryCache[c.Country] = countryInfo;
                    }

                    if (countryInfo != null)
                    {
                        dto.CountryAlpha2 = countryInfo.Alpha2;
                        dto.CountryAlpha3 = countryInfo.Alpha3;
                        dto.CurrencyCode = countryInfo.CurrencyCode;
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogError(_logger, $"Error retrieving country info for {c.Country}", ex);
                }

                // Weather info
                try
                {
                    var weather = await GetWeatherAsync(c.Name);
                    if (weather != null)
                    {
                        dto.WeatherMain = weather.Main;
                        dto.WeatherDescription = weather.Description;
                        dto.TemperatureCelsius = weather.Temperature;
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogError(_logger, $"Error retrieving weather info for {c.Name}", ex);
                }

                results.Add(dto);
            }

            AppLogger.LogActionSuccess(_logger, ApiActions.SearchCity, new { Count = results.Count });
            return Result<IEnumerable<CitySearchResultDto>>.Ok(results);
        }

        // -----------------------------
        // PRIVATE HELPERS
        // -----------------------------
       
        private async Task<CountryInfo?> GetCountryInfoAsync(string countryName)
        {
            if (string.IsNullOrWhiteSpace(countryName)) return null;

            var client = _httpFactory.CreateClient("RestCountries");
            var url = $"name/{WebUtility.UrlEncode(countryName)}?fullText=true&fields=cca2,cca3,currencies";

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            var countries = JsonSerializer.Deserialize<List<RestCountryResponse>>(json, JsonOptions);

            var country = countries?.FirstOrDefault();
            if (country == null)
                return null;

            var currencyCode = country.Currencies?.Keys.FirstOrDefault();

            return new CountryInfo(country.Cca2, country.Cca3, currencyCode);

        }

        private async Task<CountryInfo?> GetCachedCountryInfoAsync(string countryName)
        {
            if (string.IsNullOrWhiteSpace(countryName))
                return null;

            var cacheKey = $"country:{countryName.ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out CountryInfo? cached))
                return cached;

            var info = await GetCountryInfoAsync(countryName);

            _cache.Set(cacheKey, info, TimeSpan.FromHours(6));
            return info;
        }

        private static string? GetString(JsonElement el, string propName)
        {
            if (el.TryGetProperty(propName, out var p) && p.ValueKind == JsonValueKind.String)
                return p.GetString();
            return null;
        }

        private async Task<WeatherInfo?> GetWeatherAsync(string cityName)
        {
            var client = _httpFactory.CreateClient("VisualCrossing");
            var apiKey = _config["VisualCrossingMap:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) return null;

            var q = WebUtility.UrlEncode(cityName);
            var url = $"{q}/today?unitGroup=metric&include=days&key={apiKey}&contentType=json";

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();

            var weatherResponse = JsonSerializer.Deserialize<VisualCrossingResponse>(json, JsonOptions);
            var firstDay = weatherResponse?.Days?.FirstOrDefault();

            if (firstDay == null)
                return null;

            return new WeatherInfo(
                firstDay.Conditions ?? string.Empty,
                firstDay.Description ?? string.Empty,
                firstDay.Temp
            );

        }
    }
}
