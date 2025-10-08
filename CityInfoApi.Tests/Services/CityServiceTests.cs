using CityInfoApi.Services;
using CityInfoApi.Repositories;
using CityInfoApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net;
using CityInfoApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CityInfoApi.Tests.Services
{
    public class CityServiceTests
    {
        [Fact]
        public async Task SearchCityByName_Returns_Composed_Result()
        {
            // setup in-memory db
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_Search")
                .Options;

            using var db = new AppDbContext(options);
            db.Cities.Add(new City
            {
                Name = "TestTown",
                State = "TestState",
                Country = "Testland",
                TouristRating = 4,
                DateEstablished = new System.DateTime(1900, 1, 1),
                EstimatedPopulation = 12345
            });
            db.SaveChanges();

            var repo = new CityRepository(db);

            // prepare fake HTTP responses:
            var restCountriesJson = @"[{
                ""alpha2Code"": ""TL"",
                ""alpha3Code"": ""TST"",
                ""currencies"": [{ ""code"": ""TST"" }]
            }]";

            var weatherJson = @"{
                ""days"": [{ ""conditions"": ""Clear"", ""description"": ""clear sky"", ""temp"": 20.5 }]
            }";

            var factory = new FakeHttpClientFactory(new[]
            {
                ("RestCountries", restCountriesJson),
                ("VisualCrossing", weatherJson)
            });

            var inMemoryConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                {
                    { "VisualCrossingMap:ApiKey", "FAKE_KEY" }
                }).Build();

            var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<CityService>();

            var svc = new CityService(repo, factory, inMemoryConfig, logger);

            var result = await svc.SearchCityByNameAsync("TestTown");

            Assert.NotNull(result);
            var first = Assert.Single(result);
            Assert.Equal("TestTown", first.Name);
            Assert.Equal("TL", first.CountryAlpha2);
            Assert.Equal("TST", first.CountryAlpha3);
            Assert.Equal("TST", first.CurrencyCode);
            Assert.Equal("Clear", first.WeatherMain);
            Assert.Equal(20.5, first.TemperatureCelsius);
        }
    }


    public class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly System.Collections.Generic.Dictionary<string, string> _responses;
        public FakeHttpClientFactory((string name, string response)[] pairs)
        {
            _responses = pairs.ToDictionary(p => p.name, p => p.response);
        }

        public HttpClient CreateClient(string name)
        {
            if (!_responses.TryGetValue(name, out var resp)) resp = "{}";

            var handler = new FakeHttpMessageHandler(resp);
            return new HttpClient(handler) { BaseAddress = new System.Uri("http://test.local/") };
        }

        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _payload;
            public FakeHttpMessageHandler(string payload) => _payload = payload;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_payload, Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }
        }
    }
}
