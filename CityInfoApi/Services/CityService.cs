using CityInfoApi.DTOs;
using CityInfoApi.Models;
using CityInfoApi.Repositories;
using System.Net;
using System.Text.Json;

namespace CityInfoApi.Services
{
    public class CityService : ICityService
    {
        private readonly ICityRepository _repo;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<CityService> _logger;

        public CityService(ICityRepository repo, IHttpClientFactory httpFactory, IConfiguration config, ILogger<CityService> logger)
        {
            _repo = repo;
            _httpFactory = httpFactory;
            _config = config;
            _logger = logger;
        }

        public async Task<City?> AddCityAsync(AddCityRequest request) {
            var result = await _repo.AddAsync(request);

            if (result == null)
            {
                _logger.LogWarning("Duplicate city detected: {CityName}, {Country}", request.Name, request.Country);
            }

            return result;
        } 

        public async Task<bool> UpdateCityAsync(int id, int touristRating, DateTime dateEstablished, long estimatedPopulation)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            existing.TouristRating = touristRating;
            existing.DateEstablished = dateEstablished;
            existing.EstimatedPopulation = estimatedPopulation;
            await _repo.UpdateAsync(existing);
            return true;
        }

        public Task<bool> DeleteCityAsync(int id)
        {
            return DeleteInternalAsync(id);
        }

        private async Task<bool> DeleteInternalAsync(int id)
        {
            var found = await _repo.GetByIdAsync(id);
            if (found == null) return false;
            await _repo.DeleteAsync(id);
            return true;
        }

        public async Task<IEnumerable<CitySearchResultDto>> SearchCityByNameAsync(string name)
        {
            var cities = (await _repo.SearchByNameAsync(name)).ToList();

            if (!cities.Any()) return Enumerable.Empty<CitySearchResultDto>();

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
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving country info for {Country}", c.Country);
                }

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
                    _logger.LogError(ex, "Error retrieving weather info for {City}", c.Name);
                }

                results.Add(dto);
            }

            return results;
        }


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
            JsonElement root = doc.RootElement;

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

            // Build q param: city or city,country
            var q = WebUtility.UrlEncode(cityName);
            var url = $"{q}/today?unitGroup=metric&include=days&key={apiKey}&contentType=json";
            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            string mainWeatherInfo = string.Empty;
            string additionalWeatherDescription = string.Empty;
            double temp = 0;

            if (root.TryGetProperty("days", out var weatherArr) && weatherArr.ValueKind == JsonValueKind.Array && weatherArr.GetArrayLength() > 0)
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
