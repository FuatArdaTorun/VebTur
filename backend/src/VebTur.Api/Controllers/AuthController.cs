using System.IdentityModel.Tokens.Jwt;
using VebTur.Application.Auth;
using VebTur.Application.Contracts.Auth;
using VebTur.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var token = jwtTokenService.GenerateToken(user.Id, user.Email!, roles);

        return Ok(new LoginResponseDto(token.Token, token.ExpiresAtUtc, user.Email!, user.DisplayName, roles));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userId is null || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        return Ok(new CurrentUserDto(user.Id, user.Email!, user.DisplayName, roles));
    }
}
