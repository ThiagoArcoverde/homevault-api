using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;

namespace Homevault_api.Controllers;

[ApiController]
[Route("health")]
public class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var result = await healthCheckService.CheckHealthAsync(cancellationToken);

        return result.Status == HealthStatus.Healthy
            ? Ok("Healthy")
            : StatusCode(StatusCodes.Status503ServiceUnavailable, result.Status.ToString());
    }
}
