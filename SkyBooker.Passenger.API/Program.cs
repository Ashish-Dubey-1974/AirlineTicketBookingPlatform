using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SkyBooker.Passenger.API.Data;
using SkyBooker.Passenger.API.DTOs;
using SkyBooker.Passenger.API.Repositories;
using SkyBooker.Passenger.API.Services;
using SkyBooker.Passenger.API.Validators;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("SkyBooker Passenger API starting up...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Database
    builder.Services.AddDbContext<PassengerDbContext>(options =>
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
    });

    // Dependency Injection
    builder.Services.AddScoped<IPassengerRepository, PassengerRepository>();
    builder.Services.AddScoped<IPassengerService, PassengerService>();
    builder.Services.AddScoped<IValidator<PassengerRequestDto>, PassengerInfoValidator>();

    // JWT Authentication
    var jwtSecret = builder.Configuration["JwtSettings:Secret"]
        ?? throw new InvalidOperationException("JwtSettings:Secret is not configured!");
    var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "SkyBooker.Auth.API";
    var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "SkyBooker.Clients";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    // Authorization Policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
        options.AddPolicy("PassengerOnly", policy => policy.RequireRole("PASSENGER"));
        options.AddPolicy("StaffOnly", policy => policy.RequireRole("AIRLINE_STAFF"));
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SkyBooker Passenger Service API",
            Version = "v1",
            Description = "Passenger details management with FluentValidation"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header
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

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSkyBookerWeb", policy =>
            policy.WithOrigins("http://localhost:5000", "https://localhost:5001","http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });

    builder.Services.AddHealthChecks().AddDbContextCheck<PassengerDbContext>("sql-server");

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PassengerDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("PassengerDbContext migrations applied.");
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyBooker Passenger API v1"));
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseCors("AllowSkyBookerWeb");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/health");
    app.MapControllers();

    Log.Information("SkyBooker Passenger API started on port 5012");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SkyBooker Passenger API crashed!");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;