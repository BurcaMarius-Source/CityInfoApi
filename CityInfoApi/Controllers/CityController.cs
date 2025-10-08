using CityInfoApi.DTOs;
using CityInfoApi.Models;
using CityInfoApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CityInfoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CityController : ControllerBase
    {
        private readonly ICityService _svc;
        private readonly ILogger<CityController> _logger;

        public CityController(ICityService svc, ILogger<CityController> logger)
        {
            _svc = svc;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult> AddCity([FromBody] AddCityRequest request)
        {
            if (request == null)
            {
                _logger.LogWarning("AddCity called with null body");
                return BadRequest();
            }

            var created = await _svc.AddCityAsync(request);

            if (created == null)
            {
                _logger.LogWarning("Attempted to add duplicate city {CityName} in {Country}", request.Name, request.Country);
                return Conflict(new { message = $"City '{request.Name}' in '{request.Country}' already exists." });
            }

            _logger.LogInformation("Added city {CityName} with id {CityId}", created.Name, created.Id);
            return Ok(created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCity(int id, [FromBody] UpdateCityRequest req)
        {
            var ok = await _svc.UpdateCityAsync(id, req.TouristRating, req.DateEstablished, req.EstimatedPopulation);
            if (!ok)
            {
                _logger.LogWarning("Update attempted on non-existent city {CityId}", id);
                return NotFound();
            }

            _logger.LogInformation("Updated city {CityId}", id);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCity(int id)
        {
            var ok = await _svc.DeleteCityAsync(id);
            if (!ok)
            {
                _logger.LogWarning("Delete attempted on non-existent city {CityId}", id);
                return NotFound();
            }

            _logger.LogInformation("Deleted city {CityId}", id);
            return NoContent();
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<CitySearchResultDto>>> Search([FromQuery] string name)
        {
            _logger.LogInformation("Search requested for city name: {Name}", name);
            var results = await _svc.SearchCityByNameAsync(name);

            if (results == null || !results.Any())
            {
                _logger.LogInformation("No results found for {Name}", name);
                return NotFound();
            }

            return Ok(results);
        }
    }
}
