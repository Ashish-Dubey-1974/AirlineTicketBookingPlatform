// ════════════════════════════════════════════════════════════════════
// SkyBooker.Flight.API — Program.cs  (Day 3 — Complete)
// .NET 8 · ASP.NET Core · Entity Framework Core · JWT Bearer
// ════════════════════════════════════════════════════════════════════

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SkyBooker.Flights.API.Data;
using SkyBooker.Flights.API.Repositories;
using SkyBooker.Flights.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────────────────────────
// SERILOG — Structured Logging
// ──────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ──────────────────────────────────────────────────────────────────
// DATABASE — Entity Framework Core + SQL Server
// ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<FlightDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// ──────────────────────────────────────────────────────────────────
// DEPENDENCY INJECTION — Repositories & Services
// ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IFlightService, FlightService>();

// ──────────────────────────────────────────────────────────────────
// JWT BEARER AUTHENTICATION
// Shares the same JWT secret/issuer/audience as Auth.API
// ──────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSettings["Secret"]
    ?? throw new InvalidOperationException("JWT Secret is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // set true in production
    options.SaveToken            = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer           = true,
        ValidIssuer              = jwtSettings["Issuer"],
        ValidateAudience         = true,
        ValidAudience            = jwtSettings["Audience"],
        ValidateLifetime         = true,
        ClockSkew                = TimeSpan.Zero
    };
});

// ──────────────────────────────────────────────────────────────────
// AUTHORIZATION POLICIES  (roles match Auth.API JWT claims)
// ──────────────────────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",     p => p.RequireRole("ADMIN"));
    options.AddPolicy("PassengerOnly", p => p.RequireRole("PASSENGER"));
    options.AddPolicy("StaffOnly",     p => p.RequireRole("AIRLINE_STAFF"));
});

// ──────────────────────────────────────────────────────────────────
// MVC CONTROLLERS
// ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ──────────────────────────────────────────────────────────────────
// SWAGGER / OPENAPI — with JWT Bearer support
// ──────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "SkyBooker — Flight Service API",
        Version     = "v1",
        Description = "Flight schedule management, one-way/round-trip search, seat counters · Day 3"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token (without 'Bearer ' prefix)"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ──────────────────────────────────────────────────────────────────
// CORS — allow MVC web + local dev
// ──────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSkyBookerWeb", policy =>
        policy.WithOrigins(
                "http://localhost:5000",
                "https://localhost:5001",
                "http://localhost:5010")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// ──────────────────────────────────────────────────────────────────
// BUILD & MIDDLEWARE PIPELINE
// ──────────────────────────────────────────────────────────────────
var app = builder.Build();

// Auto-apply EF Core migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
    try
    {
        db.Database.Migrate();
        Log.Information("FlightDbContext migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Log.Warning("DB migration skipped (may already be up to date): {Message}", ex.Message);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyBooker Flight API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseSerilogRequestLogging();
app.UseCors("AllowSkyBookerWeb");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status  = "healthy",
    service = "SkyBooker.Flight.API",
    time    = DateTime.UtcNow
}));

app.Run();
