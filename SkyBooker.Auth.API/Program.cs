using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using SkyBooker.Auth.Data;
using SkyBooker.Auth.Repositories;
using SkyBooker.Auth.Services;

// ════════════════════════════════════════════════════════════════════
// SERILOG — Configure structured logging first (before builder)
// ════════════════════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("SkyBooker Auth API starting up...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog integration ───────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // ════════════════════════════════════════════════════════════════
    // DATABASE — Entity Framework Core + SQL Server
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddDbContext<UsersDbContext>(options =>
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });

        // Log SQL queries in development
        if (builder.Environment.IsDevelopment())
            options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
                   .EnableSensitiveDataLogging();
    });

    // ════════════════════════════════════════════════════════════════
    // DEPENDENCY INJECTION — Repositories & Services (Scoped)
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IAuthService, AuthService>();

    // ════════════════════════════════════════════════════════════════
    // AUTHENTICATION — JWT Bearer + Google OAuth2
    // ════════════════════════════════════════════════════════════════
    var jwtSecret  = builder.Configuration["JwtSettings:Secret"]
        ?? throw new InvalidOperationException("JwtSettings:Secret is not configured!");
    var jwtIssuer   = builder.Configuration["JwtSettings:Issuer"]   ?? "SkyBooker.Auth.API";
    var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "SkyBooker.Clients";

    var googleClientId     = builder.Configuration["Google:ClientId"];
    var googleClientSecret = builder.Configuration["Google:ClientSecret"];
    // Google OAuth is enabled only when real credentials are provided (not placeholders).
    // Set actual values in appsettings.json or Azure Key Vault before Day 2 implementation.
    var googleEnabled = !string.IsNullOrWhiteSpace(googleClientId)
                     && !string.IsNullOrWhiteSpace(googleClientSecret)
                     && googleClientId != "REPLACE_WITH_GOOGLE_CLIENT_ID";

    var authBuilder = builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer           = true,
                ValidIssuer              = jwtIssuer,
                ValidateAudience         = true,
                ValidAudience            = jwtAudience,
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero  // no clock drift tolerance
            };

            // JWT events for logging
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    Log.Warning("JWT auth failed: {Error}", ctx.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = ctx =>
                {
                    var userId = ctx.Principal?.FindFirst("userId")?.Value;
                    Log.Debug("JWT validated for UserId={UserId}", userId);
                    return Task.CompletedTask;
                }
            };
        });

    if (googleEnabled)
    {
        authBuilder.AddGoogle(googleOptions =>
        {
            googleOptions.ClientId     = googleClientId!;
            googleOptions.ClientSecret = googleClientSecret!;
        });
        Log.Information("Google OAuth2 enabled.");
    }
    else
    {
        Log.Warning("Google OAuth2 is DISABLED — set real Google:ClientId and Google:ClientSecret to enable. (Day 2 task)");
    }

    // ════════════════════════════════════════════════════════════════
    // AUTHORIZATION — Role-based policies
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly",     policy => policy.RequireRole("ADMIN"));
        options.AddPolicy("PassengerOnly", policy => policy.RequireRole("PASSENGER"));
        options.AddPolicy("StaffOnly",     policy => policy.RequireRole("AIRLINE_STAFF"));
        options.AddPolicy("StaffOrAdmin",  policy => policy.RequireRole("AIRLINE_STAFF", "ADMIN"));
    });

    // ════════════════════════════════════════════════════════════════
    // CONTROLLERS & API BEHAVIOUR
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            // Return custom error format instead of default ProblemDetails
            options.SuppressModelStateInvalidFilter = true;
        });

    // ════════════════════════════════════════════════════════════════
    // CORS — Allow SkyBooker.Web to call this API
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("SkyBookerCors", policy =>
        {
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? ["http://localhost:5000", "https://localhost:5001"];

            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // ════════════════════════════════════════════════════════════════
    // SWAGGER / OPENAPI
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "SkyBooker Auth API",
            Version     = "v1",
            Description = "Authentication and User Management service for SkyBooker platform",
            Contact     = new OpenApiContact { Name = "SkyBooker Dev Team" }
        });

        // Add JWT Bearer auth to Swagger UI
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header. Enter: Bearer {token}",
            Name        = "Authorization",
            In          = ParameterLocation.Header,
            Type        = SecuritySchemeType.ApiKey,
            Scheme      = "Bearer"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Include XML comments (enable in .csproj: <GenerateDocumentationFile>true</GenerateDocumentationFile>)
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath);
    });

    // ════════════════════════════════════════════════════════════════
    // HEALTH CHECKS
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddHealthChecks().AddDbContextCheck<UsersDbContext>("sql-server");

    // ════════════════════════════════════════════════════════════════
    // BUILD THE APP
    // ════════════════════════════════════════════════════════════════
    var app = builder.Build();

    // ── Auto-run EF Core migrations on startup ────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        Log.Information("Applying EF Core migrations for UsersDbContext...");
        await db.Database.MigrateAsync();
        Log.Information("Migrations applied successfully.");
    }

    // ════════════════════════════════════════════════════════════════
    // MIDDLEWARE PIPELINE (ORDER MATTERS!)
    // ════════════════════════════════════════════════════════════════

    // 1. Swagger (dev only)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyBooker Auth API v1");
            c.RoutePrefix = string.Empty;  // Swagger at root URL
        });
    }

    // 2. HTTPS redirect
    app.UseHttpsRedirection();

    // 3. Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    // 4. CORS (before auth)
    app.UseCors("SkyBookerCors");

    // 5. Authentication & Authorization (ORDER CRITICAL!)
    app.UseAuthentication();
    app.UseAuthorization();

    // 6. Health check endpoint
    app.MapHealthChecks("/health");

    // 7. Controllers
    app.MapControllers();

    Log.Information("SkyBooker Auth API started. Swagger: http://localhost:{Port}/", 5001);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SkyBooker Auth API crashed on startup!");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
