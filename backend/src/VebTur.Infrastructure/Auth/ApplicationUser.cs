using Microsoft.AspNetCore.Identity;

namespace VebTur.Infrastructure.Auth;

public class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.CreateVersion7();
    }

    public required string DisplayName { get; set; }
}
