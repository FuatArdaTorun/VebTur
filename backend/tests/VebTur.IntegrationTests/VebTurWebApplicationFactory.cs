using VebTur.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace VebTur.IntegrationTests;

/// <summary>
/// Boots the real API pipeline (Program.cs, real middleware, real controllers) against a real,
/// dedicated Postgres database ("vebtur_test") on the same Docker instance dev uses — not a
/// mock/in-memory provider — per this project's integration-testing philosophy. The JWT signing
/// key and admin credentials are test-only values set here rather than the developer machine's
/// User Secrets. The database credentials come from the same POSTGRES_* settings docker compose
/// uses (environment variables, or the repository's .env file), so no password lives in source
/// control and the suite runs on any machine or CI runner with Postgres reachable on localhost.
/// </summary>
/// <summary>
/// Every test class that needs the API host shares this one fixture instance via
/// <c>[Collection(Name)]</c> below (not one <see cref="VebTurWebApplicationFactory"/> per class) —
/// two independent instances would each migrate/drop the same hardcoded "vebtur_test" database,
/// and xUnit runs different test classes in parallel by default, racing those against each other.
/// </summary>
[CollectionDefinition(Name)]
public class VebTurApiCollection : ICollectionFixture<VebTurWebApplicationFactory>
{
    public const string Name = "VebTur API";
}

public class VebTurWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@vebtur-integration-tests.local";
    public const string AdminPassword = "IntegrationTest1!Password";

    private static readonly string TestConnectionString = BuildTestConnectionString();

    private static string BuildTestConnectionString()
    {
        var dotEnv = ReadDotEnv();
        string Setting(string key, string? fallback = null) =>
            Environment.GetEnvironmentVariable(key)
            ?? dotEnv.GetValueOrDefault(key)
            ?? fallback
            ?? throw new InvalidOperationException(
                $"Integration tests need {key}: set it in the repository's .env file (see .env.example) or as an environment variable.");

        return new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Port = int.Parse(Setting("POSTGRES_PORT", "5432")),
            Database = "vebtur_test",
            Username = Setting("POSTGRES_USER"),
            Password = Setting("POSTGRES_PASSWORD"),
        }.ConnectionString;
    }

    /// <summary>Reads the KEY=VALUE lines of the .env file at the repository root, if there is one.</summary>
    private static Dictionary<string, string> ReadDotEnv()
    {
        var values = new Dictionary<string, string>();
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, ".env");
            if (!File.Exists(path))
            {
                continue;
            }

            foreach (var line in File.ReadAllLines(path).Select(l => l.Trim()))
            {
                if (line.Length == 0 || line.StartsWith('#') || !line.Contains('='))
                {
                    continue;
                }

                var parts = line.Split('=', 2);
                values[parts[0].Trim()] = parts[1].Trim();
            }

            break;
        }

        return values;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // UseSetting (not ConfigureAppConfiguration + AddInMemoryCollection) — the latter's
        // in-memory source ends up *lower* priority than this machine's own dev User Secrets
        // in WebApplicationFactory's minimal-hosting pipeline, so it silently loses (tests were
        // authenticating against the real local dev JWT signing key instead of this override).
        // UseSetting is documented to always take highest precedence, which is what a test host
        // actually needs.
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-at-least-32-bytes-long-for-hs256");
        builder.UseSetting("Jwt:Issuer", "VebTur.IntegrationTests");
        builder.UseSetting("Jwt:Audience", "VebTur.IntegrationTests.Frontend");
        builder.UseSetting("Jwt:ExpiryHours", "1");
        builder.UseSetting("Admin:Email", AdminEmail);
        builder.UseSetting("Admin:Password", AdminPassword);
        builder.UseSetting("Admin:DisplayName", "Integration Test Admin");
    }

    public async Task InitializeAsync()
    {
        // Migrate via a standalone DbContext, deliberately NOT touching this.Services first:
        // WebApplicationFactory builds and starts the real host (running Program.cs's dev-only
        // HotelSeeder/IdentitySeeder block) the moment Services is first accessed. On a brand
        // new "vebtur_test" database the "Hotels"/Identity tables don't exist yet, so that
        // seeding would fail immediately — the schema has to exist *before* the host starts.
        // This also directly exercises "migrations apply cleanly against a real database."
        var optionsBuilder = new DbContextOptionsBuilder<VebTurDbContext>().UseNpgsql(TestConnectionString);
        await using var migrationContext = new VebTurDbContext(optionsBuilder.Options);
        await migrationContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();

        var optionsBuilder = new DbContextOptionsBuilder<VebTurDbContext>().UseNpgsql(TestConnectionString);
        await using var cleanupContext = new VebTurDbContext(optionsBuilder.Options);
        await cleanupContext.Database.EnsureDeletedAsync();
    }
}
