using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SkyBooker.Payment.API.Data;
using SkyBooker.Payment.API.Services;
using SkyBooker.Payment.API.Repositories;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("SkyBooker Payment API starting up...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Database
    builder.Services.AddDbContext<PaymentDbContext>(options =>
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
    });

    // HTTP Clients
    builder.Services.AddHttpClient("BookingService", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:BookingService"] ?? "http://localhost:5010");
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    // Dependency Injection
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IPaymentService, PaymentService>();
    builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

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
            Title = "SkyBooker Payment Service API",
            Version = "v1",
            Description = "Payment processing with Razorpay/Stripe integration"
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
            policy.WithOrigins("http://localhost:5000", "https://localhost:5001","http://localhost:4200", "http://127.0.0.1:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });

    builder.Services.AddHealthChecks().AddDbContextCheck<PaymentDbContext>("sql-server");

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("PaymentDbContext migrations applied.");
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyBooker Payment API v1"));
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseCors("AllowSkyBookerWeb");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/health");
    app.MapControllers();

    Log.Information("SkyBooker Payment API started on port 5013");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SkyBooker Payment API crashed!");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
