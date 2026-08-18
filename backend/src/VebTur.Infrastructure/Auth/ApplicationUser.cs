using VebTur.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace VebTur.Infrastructure.Auth;

public class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.CreateVersion7();
    }

    public required string DisplayName { get; set; }

    // All optional — set via the customer's own profile page, never required at registration.
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Gender? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}
