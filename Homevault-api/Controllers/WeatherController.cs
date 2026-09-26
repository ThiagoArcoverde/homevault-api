using Asp.Versioning;
using Homevault.Application.Ports;
using Homevault.Application.Weather;
using Homevault.Domain.Entities;
using Homevault.Infrastructure.Weather;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Homevault_api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/weather")]
public sealed class WeatherController(
    GetWeather getWeather,
    IOptions<WeatherOptions> options) : ControllerBase
{
    [HttpGet("current")]
    [ProducesResponseType(typeof(WeatherObservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var observation = await getWeather.GetLatestAsync(
            CreateLocation(options.Value.Location),
            cancellationToken);

        return observation is null
            ? NotFound()
            : Ok(WeatherObservationResponse.FromObservation(observation));
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<WeatherObservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 200)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Os parâmetros de paginação são inválidos.",
                Detail = "Page deve ser maior que zero e pageSize deve estar entre 1 e 200."
            });
        }

        if (from.HasValue && to.HasValue && from > to)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "O período informado é inválido.",
                Detail = "A data inicial deve ser menor ou igual à data final."
            });
        }

        var observations = await getWeather.GetHistoryAsync(
            CreateLocation(options.Value.Location),
            from,
            to,
            page,
            pageSize,
            cancellationToken);

        return Ok(observations.Select(WeatherObservationResponse.FromObservation));
    }

    private static WeatherLocation CreateLocation(LocationOptions options)
    {
        return new WeatherLocation(
            options.City,
            options.State,
            options.Latitude,
            options.Longitude,
            options.TimeZone);
    }
}

public sealed record WeatherObservationResponse(
    Guid Id,
    string City,
    string State,
    decimal TemperatureCelsius,
    decimal RelativeHumidity,
    string Condition,
    bool IsDay,
    DateTimeOffset ObservedAt,
    DateTimeOffset CollectedAt)
{
    public static WeatherObservationResponse FromObservation(WeatherObservation observation)
    {
        return new WeatherObservationResponse(
            observation.Id,
            observation.City,
            observation.State,
            observation.TemperatureCelsius,
            observation.RelativeHumidity,
            observation.Condition.ToString(),
            observation.IsDay,
            observation.ObservedAt,
            observation.CollectedAt);
    }
}
