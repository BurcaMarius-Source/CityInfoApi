using CityInfoApi.Common;
using CityInfoApi.DTOs;
using CityInfoApi.Models;
using CityInfoApi.Repositories;
using System.Net;
using System.Text.Json;

namespace CityInfoApi.Services
{
    public class CityService(
        ICityRepository repo,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<CityService> logger) : ICityService
    {
        private readonly ICityRepository _repo = repo;
        private readonly IHttpClientFactory _httpFactory = httpFactory;
        private readonly IConfiguration _config = config;
        private readonly ILogger<CityService> _logger = logger;

        // -----------------------------
        // ADD CITY
        // -----------------------------
        public async Task<Result<City>> AddCityAsync(AddCityRequest request)
        {
            AppLogger.LogActionStart(_logger, "AddCity", request);

            var addedCity = await _repo.AddAsync(request);
            if (addedCity == null)
            {
                AppLogger.LogWarning(_logger, ApiMessages.DuplicateCity, new { request.Name, request.Country });
                return Result<City>.Conflict(ApiMessages.DuplicateCity);
            }

            AppLogger.LogActionSuccess(_logger, "AddCity", new { addedCity.Id, addedCity.Name });
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
            AppLogger.LogActionStart(_logger, "UpdateCity", new { id });

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
            AppLogger.LogActionSuccess(_logger, "UpdateCity", new { id });

            return Result<City>.NoContent(ApiMessages.CityUpdated);
        }

        // -----------------------------
        // DELETE CITY
        // -----------------------------
        public async Task<Result<City>> DeleteCityAsync(int id)
        {
            AppLogger.LogActionStart(_logger, "DeleteCity", new { id });

            var city = await _repo.GetByIdAsync(id);
            if (city == null)
            {
                AppLogger.LogWarning(_logger, ApiMessages.CityNotFound, new { id });
                return Result<City>.NotFound(ApiMessages.CityNotFound);
            }

            await _repo.DeleteAsync(id);
            AppLogger.LogActionSuccess(_logger, "DeleteCity", new { id });

            return Result<City>.NoContent(ApiMessages.CityDeleted);
        }

        // -----------------------------
        // SEARCH CITY
        // -----------------------------
        public async Task<Result<IEnumerable<CitySearchResultDto>>> SearchCityByNameAsync(string name)
        {
            AppLogger.LogActionStart(_logger, "SearchCity", new { name });

            var cities = (await _repo.SearchByNameAsync(name)).ToList();
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
                    var countryInfo = await GetCountryInfoAsync(c.Country);
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

            AppLogger.LogActionSuccess(_logger, "SearchCity", new { Count = results.Count });
            return Result<IEnumerable<CitySearchResultDto>>.Ok(results);
        }

        // -----------------------------
        // PRIVATE HELPERS
        // -----------------------------
        private record CountryInfo(string? Alpha2, string? Alpha3, string? CurrencyCode);

        private async Task<CountryInfo?> GetCountryInfoAsync(string countryName)
        {
            if (string.IsNullOrWhiteSpace(countryName)) return null;

            var client = _httpFactory.CreateClient("RestCountries");
            var url = $"name/{WebUtility.UrlEncode(countryName)}?fullText=true";

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;

            var text = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                root = root[0];

            string? alpha2 = GetString(root, "alpha2Code") ?? GetString(root, "cca2");
            string? alpha3 = GetString(root, "alpha3Code") ?? GetString(root, "cca3");

            string? currencyCode = null;
            if (root.TryGetProperty("currencies", out var currenciesElem))
            {
                if (currenciesElem.ValueKind == JsonValueKind.Array && currenciesElem.GetArrayLength() > 0)
                {
                    var first = currenciesElem[0];
                    currencyCode = GetString(first, "code") ?? GetString(first, "symbol") ?? first.ToString();
                }
                else if (currenciesElem.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in currenciesElem.EnumerateObject())
                    {
                        currencyCode = prop.Name;
                        break;
                    }
                }
            }

            return new CountryInfo(alpha2, alpha3, currencyCode);
        }

        private static string? GetString(JsonElement el, string propName)
        {
            if (el.TryGetProperty(propName, out var p) && p.ValueKind == JsonValueKind.String)
                return p.GetString();
            return null;
        }

        private record WeatherInfo(string Main, string Description, double Temperature);

        private async Task<WeatherInfo?> GetWeatherAsync(string cityName)
        {
            var client = _httpFactory.CreateClient("VisualCrossing");
            var apiKey = _config["VisualCrossingMap:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) return null;

            var q = WebUtility.UrlEncode(cityName);
            var url = $"{q}/today?unitGroup=metric&include=days&key={apiKey}&contentType=json";

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            string mainWeatherInfo = string.Empty;
            string additionalWeatherDescription = string.Empty;
            double temp = 0;

            if (root.TryGetProperty("days", out var weatherArr) &&
                weatherArr.ValueKind == JsonValueKind.Array &&
                weatherArr.GetArrayLength() > 0)
            {
                var weatherElement = weatherArr[0];
                mainWeatherInfo = GetString(weatherElement, "conditions") ?? string.Empty;
                additionalWeatherDescription = GetString(weatherElement, "description") ?? string.Empty;

                if (weatherElement.TryGetProperty("temp", out var tempElem) && tempElem.TryGetDouble(out var t))
                {
                    temp = t;
                }
            }

            return new WeatherInfo(mainWeatherInfo, additionalWeatherDescription, temp);
        }
    }
}
