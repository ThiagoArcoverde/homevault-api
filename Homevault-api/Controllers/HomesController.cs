using Asp.Versioning;
using Homevault.Application.Homes;
using Microsoft.AspNetCore.Mvc;

namespace Homevault_api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/homes")]
public class HomesController(CreateHome createHome) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        CreateHomeCommand command,
        CancellationToken cancellationToken)
    {
        var home = await createHome.ExecuteAsync(command, cancellationToken);

        return Created($"/api/v1/homes/{home.Id}", new
        {
            home.Id,
            home.Name,
            home.CreatedAt
        });
    }
}
