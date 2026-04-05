// ════════════════════════════════════════════════════════════════════
// SkyBooker.Notification.API — Program.cs
// TODO Day 8: Implement full DI, EF Core, Controllers
// ════════════════════════════════════════════════════════════════════
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// TODO: Add DbContext, Repositories, Services, JWT Auth

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "SkyBooker.Notification.API" }));

app.Run();
