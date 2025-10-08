namespace CityInfoApi.DTOs
{
    public class CitySearchResultDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string State { get; set; } = null!;
        public string Country { get; set; } = null!;
        public int TouristRating { get; set; }
        public DateTime DateEstablished { get; set; }
        public long EstimatedPopulation { get; set; }

        public string? CountryAlpha2 { get; set; }    
        public string? CountryAlpha3 { get; set; }    
        public string? CurrencyCode { get; set; }   

        public string? WeatherMain { get; set; }
        public string? WeatherDescription { get; set; }
        public double? TemperatureCelsius { get; set; }
    }
}
