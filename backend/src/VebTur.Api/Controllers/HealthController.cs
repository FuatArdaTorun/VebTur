using VebTur.Application.Contracts;
using VebTur.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController(VebTurDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HealthStatusDto>> Get(CancellationToken cancellationToken)
    {
        var reachable = await db.Database.CanConnectAsync(cancellationToken);
        return Ok(new HealthStatusDto("healthy", reachable, DateTimeOffset.UtcNow));
    }
}
