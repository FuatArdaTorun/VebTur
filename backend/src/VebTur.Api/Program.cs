using System.IdentityModel.Tokens.Jwt;
using System.Text;
using VebTur.Api.ExceptionHandling;
using VebTur.Application.Admin;
using VebTur.Application.Auth;
using VebTur.Application.Hotels;
using VebTur.Application.Reservations;
using VebTur.Infrastructure.Admin;
using VebTur.Infrastructure.Auth;
using VebTur.Infrastructure.Hotels;
using VebTur.Infrastructure.Persistence;
using VebTur.Infrastructure.Persistence.Seed;
using VebTur.Infrastructure.Reservations;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

const string AngularDevCorsPolicy = "AngularDev";

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT returned from POST /api/v1/auth/login.",
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
    });
});

builder.Services.AddDbContext<VebTurDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<VebTurDbContext>();

builder.Services.AddScoped<IHotelQueryService, HotelQueryService>();
builder.Services.AddScoped<IAdminHotelService, AdminHotelService>();
builder.Services.AddScoped<IAdminAmenityService, AdminAmenityService>();
builder.Services.AddScoped<IReservationRequestService, ReservationRequestService>();
builder.Services.AddScoped<IAdminReservationService, AdminReservationService>();
builder.Services.AddScoped<IAdminNotificationService, AdminNotificationService>();
builder.Services.AddScoped<IHotelNotificationService, DemoHotelNotificationService>();

builder.Services.AddValidatorsFromAssembly(typeof(IHotelQueryService).Assembly);

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<VebTurDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        // Without this, claims get silently rewritten to ClaimTypes' long legacy URIs
        // (e.g. "role" -> "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"),
        // which would not match the short-name RoleClaimType/NameClaimType set below.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
            // Match the short claim names JwtTokenService actually writes (MapInboundClaims
            // defaults to false as of .NET 8, so claims are not rewritten to ClaimTypes' long URIs).
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role",
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.

// "Admin"/"Customer" roles must exist in every environment (not just Development) — public
// self-registration assigns "Customer" on first use, so the role can't be dev-seed-only.
using (var roleSeedScope = app.Services.CreateScope())
{
    var roleManager = roleSeedScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    await IdentitySeeder.EnsureRolesExistAsync(roleManager);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var seedScope = app.Services.CreateScope();
    var db = seedScope.ServiceProvider.GetRequiredService<VebTurDbContext>();
    await HotelSeeder.SeedAsync(db);

    var roleManager = seedScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    var userManager = seedScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var configuration = seedScope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = seedScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await IdentitySeeder.SeedAsync(roleManager, userManager, configuration, logger);
}

app.UseHttpsRedirection();

app.UseCors(AngularDevCorsPolicy);

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

// Exposes the top-level-statements Program class to WebApplicationFactory<Program> in
// VebTur.IntegrationTests (that type is internal by default otherwise).
public partial class Program;
