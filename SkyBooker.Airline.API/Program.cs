using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SkyBooker.Airline.Data;
using SkyBooker.Airline.Repositories;
using SkyBooker.Airline.Services;

// ════════════════════════════════════════════════════════════════════
// SERILOG — Bootstrap logger (before builder)
// ════════════════════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("SkyBooker Airline API starting up...");

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
    builder.Services.AddDbContext<AirlineDbContext>(options =>
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

        if (builder.Environment.IsDevelopment())
            options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
                   .EnableSensitiveDataLogging();
    });

    // ════════════════════════════════════════════════════════════════
    // DEPENDENCY INJECTION — Repository & Service (Scoped)
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddScoped<IAirlineRepository, AirlineRepository>();
    builder.Services.AddScoped<IAirlineService, AirlineService>();

    // ════════════════════════════════════════════════════════════════
    // AUTHENTICATION — JWT Bearer (shared secret with Auth.API)
    // ════════════════════════════════════════════════════════════════
    var jwtSecret   = builder.Configuration["JwtSettings:Secret"]
        ?? throw new InvalidOperationException("JwtSettings:Secret is not configured!");
    var jwtIssuer   = builder.Configuration["JwtSettings:Issuer"]   ?? "SkyBooker.Auth.API";
    var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "SkyBooker.Clients";

    builder.Services
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
                ClockSkew                = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    Log.Warning("JWT auth failed: {Error}", ctx.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });

    // ════════════════════════════════════════════════════════════════
    // AUTHORIZATION — Role-based policies (same as Auth.API)
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly",    policy => policy.RequireRole("ADMIN"));
        options.AddPolicy("PassengerOnly",policy => policy.RequireRole("PASSENGER"));
        options.AddPolicy("StaffOnly",    policy => policy.RequireRole("AIRLINE_STAFF"));
        options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("AIRLINE_STAFF", "ADMIN"));
    });

    // ════════════════════════════════════════════════════════════════
    // CONTROLLERS
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(o => o.SuppressModelStateInvalidFilter = true);

    // ════════════════════════════════════════════════════════════════
    // CORS
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
            Title       = "SkyBooker Airline & Airport API",
            Version     = "v1",
            Description = "Airline and Airport master data management service for SkyBooker"
        });

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

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath);
    });

    // ════════════════════════════════════════════════════════════════
    // HEALTH CHECKS
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AirlineDbContext>("sql-server");

    // ════════════════════════════════════════════════════════════════
    // BUILD THE APP
    // ════════════════════════════════════════════════════════════════
    
    var app = builder.Build();

    // ── Auto-run EF Core migrations on startup ────────────────────────────────
    // using (var scope = app.Services.CreateScope())
    // {
    //     var db = scope.ServiceProvider.GetRequiredService<AirlineDbContext>();
    //     Log.Information("Applying EF Core migrations for AirlineDbContext...");
    //     await db.Database.MigrateAsync();
    //     Log.Information("Migrations applied successfully.");
    // }

    // ════════════════════════════════════════════════════════════════
    // MIDDLEWARE PIPELINE
    // ════════════════════════════════════════════════════════════════
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyBooker Airline API v1");
        c.RoutePrefix = "swagger";
    });

    app.UseHttpsRedirection();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseCors("SkyBookerCors");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health");
    app.MapControllers();

    Log.Information("SkyBooker Airline API started. Swagger: http://localhost:5008/swagger");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SkyBooker Airline API crashed on startup!");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
