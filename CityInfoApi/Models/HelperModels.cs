namespace CityInfoApi.Models
{
    public record UpdateCityRequest(int TouristRating, DateTime DateEstablished, long EstimatedPopulation);

    public record AddCityRequest
    {
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int TouristRating { get; set; }
        public DateTime DateEstablished { get; set; }
        public int EstimatedPopulation { get; set; }
    }
    internal record WeatherInfo(string Main, string Description, double Temperature);
    internal record CountryInfo(string? Alpha2, string? Alpha3, string? CurrencyCode);
}
