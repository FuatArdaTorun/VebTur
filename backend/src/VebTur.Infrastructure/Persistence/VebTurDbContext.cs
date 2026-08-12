using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Persistence;

public class VebTurDbContext(DbContextOptions<VebTurDbContext> options) : DbContext(options)
{
}
