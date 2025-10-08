using CityInfoApi.Common;
using CityInfoApi.DTOs;
using CityInfoApi.Models;
using CityInfoApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CityInfoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CityController : ControllerBase
    {
        private readonly ICityService _cityService;
        private readonly ILogger<CityController> _logger;

        public CityController(ICityService cityService, ILogger<CityController> logger)
        {
            _cityService = cityService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> AddCity([FromBody] AddCityRequest request)
        {
            if (request == null)
            {
                AppLogger.LogWarning(_logger, ApiMessages.InvalidRequest);
                return BadRequest(Result<City>.BadRequest(ApiMessages.InvalidRequest));
            }

            AppLogger.LogActionStart(_logger, ApiActions.AddCity, request);
            var result = await _cityService.AddCityAsync(request);

            AppLogger.LogActionSuccess(_logger, ApiActions.AddCity, new { request.Name, result.StatusCode });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCity(int id, [FromBody] UpdateCityRequest request)
        {
            if (request == null)
            {
                AppLogger.LogWarning(_logger, ApiMessages.InvalidRequest, new { Id = id });
                return BadRequest(Result<City>.BadRequest(ApiMessages.InvalidRequest));
            }

            AppLogger.LogActionStart(_logger, ApiActions.UpdateCity, new { Id = id });
            var result = await _cityService.UpdateCityAsync(id, request.TouristRating, request.DateEstablished, request.EstimatedPopulation);

            if (!result.Success)
            {
                AppLogger.LogWarning(_logger, result.Message ?? ApiMessages.CityNotFound, new { Id = id });
                return StatusCode(result.StatusCode, result);
            }

            AppLogger.LogActionSuccess(_logger, ApiActions.UpdateCity, new { Id = id });
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCity(int id)
        {
            AppLogger.LogActionStart(_logger, ApiActions.DeleteCity, new { Id = id });
            var result = await _cityService.DeleteCityAsync(id);

            if (!result.Success)
            {
                AppLogger.LogWarning(_logger, result.Message ?? ApiMessages.CityNotFound, new { Id = id });
                return StatusCode(result.StatusCode, result);
            }

            AppLogger.LogActionSuccess(_logger, ApiActions.DeleteCity, new { Id = id });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                AppLogger.LogWarning(_logger, ApiMessages.SearchEmpty);
                return BadRequest(Result<IEnumerable<CitySearchResultDto>>.BadRequest(ApiMessages.SearchEmpty));
            }

            AppLogger.LogActionStart(_logger, ApiActions.SearchCity, new { Name = name });
            var result = await _cityService.SearchCityByNameAsync(name);

            if (!result.Success || result.Data == null || !result.Data.Any())
            {
                AppLogger.LogWarning(_logger, ApiMessages.NoResults, new { Name = name });
                return NotFound(Result<IEnumerable<CitySearchResultDto>>.NotFound(ApiMessages.NoResults));
            }

            AppLogger.LogActionSuccess(_logger, ApiActions.SearchCity, new { Name = name, Count = result.Data.Count() });
            return Ok(result);
        }
    }
}
