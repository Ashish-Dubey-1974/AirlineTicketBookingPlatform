using System.Text;
using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SkyBooker.Bookings.API.Consumers;
using SkyBooker.Bookings.API.Data;
using SkyBooker.Bookings.API.Repositories;
using SkyBooker.Bookings.API.Services;
using Polly;
using Polly.Extensions.Http;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("SkyBooker Booking API starting up...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Database
    builder.Services.AddDbContext<BookingDbContext>(options =>
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions
                .EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)
                .MigrationsHistoryTable("__EFMigrationsHistory", "dbo"));
    });

    // HTTP Clients for inter-service communication
    builder.Services.AddHttpClient("FlightService", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:FlightService"] ?? "http://localhost:5009");
        client.Timeout = TimeSpan.FromSeconds(30);
    }).AddPolicyHandler(GetRetryPolicy());

    builder.Services.AddHttpClient("SeatService", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:SeatService"] ?? "http://localhost:5011");
        client.Timeout = TimeSpan.FromSeconds(30);
    }).AddPolicyHandler(GetRetryPolicy());

    builder.Services.AddHttpClient("PassengerService", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:PassengerService"] ?? "http://localhost:5012");
        client.Timeout = TimeSpan.FromSeconds(30);
    }).AddPolicyHandler(GetRetryPolicy());

    // MassTransit + RabbitMQ for async events
    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<BookingConfirmedEventConsumer>();
        
        x.UsingRabbitMq((context, cfg) =>
        {
            var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
            cfg.Host(host, "/", h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            });
            
            cfg.ConfigureEndpoints(context);
        });
    });

    // Dependency Injection
    builder.Services.AddScoped<IBookingRepository, BookingRepository>();
    builder.Services.AddScoped<IBookingService, BookingService>();

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
        options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("AIRLINE_STAFF", "ADMIN"));
    });

    // Controllers
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SkyBooker Booking Service API",
            Version = "v1",
            Description = "Booking lifecycle, PNR generation, EF Core transactions, ancillary add-ons"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token"
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

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSkyBookerWeb", policy =>
            policy.WithOrigins("http://localhost:5000", "https://localhost:5001", "http://localhost:5010")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());
    });

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<BookingDbContext>("sql-server");

    var app = builder.Build();

    // Auto-migrate database
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("BookingDbContext migrations applied.");
    }

    // Middleware
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyBooker Booking API v1"));
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseCors("AllowSkyBookerWeb");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/health");
    app.MapControllers();

    Log.Information("SkyBooker Booking API started");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SkyBooker Booking API crashed on startup!");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;

// Polly retry policy for resilience
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            Log.Warning("Retry {RetryAttempt} after {Timespan}s due to {Error}", 
                retryAttempt, timespan.TotalSeconds, 
                outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
        });
}