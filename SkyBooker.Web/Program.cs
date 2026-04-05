// ════════════════════════════════════════════════════════════════════
// SkyBooker.Web — ASP.NET Core MVC Program.cs
// TODO Day 8-9: Implement full CustomerController, AirlineController, AdminController
// ════════════════════════════════════════════════════════════════════
var builder = WebApplication.CreateBuilder(args);

// Add MVC with Razor views
builder.Services.AddControllersWithViews();

// TODO Day 8: Register IHttpClientFactory typed clients to call microservice APIs
// builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(c => {
//     c.BaseAddress = new Uri(builder.Configuration["Services:AuthApi"]);
// });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
