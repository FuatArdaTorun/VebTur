using System.IdentityModel.Tokens.Jwt;
using VebTur.Api.Validation;
using VebTur.Application.Auth;
using VebTur.Application.Contracts.Auth;
using VebTur.Domain.Enums;
using VebTur.Infrastructure.Auth;
using VebTur.Infrastructure.Persistence.Seed;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    IConfiguration configuration,
    IValidator<RegisterRequestDto> registerValidator,
    IValidator<UpdateProfileRequestDto> updateProfileValidator,
    IValidator<ChangePasswordRequestDto> changePasswordValidator,
    IValidator<ForgotPasswordRequestDto> forgotPasswordValidator,
    IValidator<ResetPasswordRequestDto> resetPasswordValidator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponseDto>> Register(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var validation = await registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            DisplayName = request.DisplayName,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        await userManager.AddToRoleAsync(user, IdentitySeeder.CustomerRoleName);

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var token = jwtTokenService.GenerateToken(user.Id, user.Email!, roles);

        return Ok(new LoginResponseDto(token.Token, token.ExpiresAtUtc, user.Email!, user.DisplayName, roles));
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.EmailOrUsername) ?? await userManager.FindByNameAsync(request.EmailOrUsername);
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
        return Ok(MapToCurrentUserDto(user, roles));
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> UpdateMe(UpdateProfileRequestDto request, CancellationToken cancellationToken)
    {
        var validation = await updateProfileValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

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

        user.PhoneNumber = request.PhoneNumber;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Gender = request.Gender is null ? null : Enum.Parse<Gender>(request.Gender, ignoreCase: true);
        user.DateOfBirth = request.DateOfBirth;

        // Set last — SetUserNameAsync validates uniqueness (fails with a "DuplicateUserName"
        // error if another account already has it) and persists the whole tracked entity in one
        // save, so the fields set above ride along with it.
        var updateResult = await userManager.SetUserNameAsync(user, request.UserName);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        return Ok(MapToCurrentUserDto(user, roles));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword(ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var validation = await changePasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

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

        var changeResult = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!changeResult.Succeeded)
        {
            foreach (var error in changeResult.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponseDto>> ForgotPassword(ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var validation = await forgotPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        const string message = "If an account exists for that email, password reset instructions have been sent.";

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            // Same generic message either way — doesn't reveal whether the email is registered.
            return Ok(new ForgotPasswordResponseDto(message, null));
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
        var resetLink = $"{frontendBaseUrl}/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";

        // VebTur has no real email infrastructure — the reset link is returned
        // directly instead of emailed, clearly marked as demo behavior by the frontend.
        return Ok(new ForgotPasswordResponseDto(message, resetLink));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword(ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var validation = await resetPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "This password reset link is invalid or has expired.");
            return ValidationProblem(ModelState);
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        return NoContent();
    }

    private static CurrentUserDto MapToCurrentUserDto(ApplicationUser user, IReadOnlyList<string> roles) =>
        new(
            user.Id,
            user.Email!,
            user.UserName!,
            user.DisplayName,
            user.PhoneNumber,
            user.FirstName,
            user.LastName,
            user.Gender?.ToString(),
            user.DateOfBirth,
            roles);
}
