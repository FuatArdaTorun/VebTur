using VebTur.Application.Admin;
using VebTur.Application.Contracts.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController(IAdminDashboardService adminDashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminDashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await adminDashboardService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }
}
