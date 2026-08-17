using System.IdentityModel.Tokens.Jwt;
using VebTur.Api.Validation;
using VebTur.Application.Contracts.Reviews;
using VebTur.Application.Reviews;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers;

/// <summary>
/// Public by design (no class-level [Authorize]) — anyone can read a hotel's reviews. Only
/// Create requires auth. GetReviews reads the sub claim if present (without requiring one) so an
/// authenticated caller also gets their own reviewable reservations back in the same response.
/// </summary>
[ApiController]
[Route("api/v1/hotels/{hotelId:guid}/reviews")]
public class ReviewsController(IReviewService reviewService, IValidator<CreateReviewDto> createValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HotelReviewsResponseDto>> GetReviews(Guid hotelId, CancellationToken cancellationToken)
    {
        var result = await reviewService.GetHotelReviewsAsync(hotelId, GetCurrentUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReviewDto>> CreateReview(Guid hotelId, CreateReviewDto dto, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        var created = await reviewService.CreateAsync(hotelId, RequireCurrentUserId(), dto, cancellationToken);
        return Ok(created);
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return sub is not null && Guid.TryParse(sub, out var id) ? id : null;
    }

    private Guid RequireCurrentUserId() => GetCurrentUserId()!.Value;
}
