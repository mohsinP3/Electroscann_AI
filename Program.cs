using ElectroScanAI.Models.Entities;
using Electroscann_ai.Data;
using Electroscann_ai.Models;
using Electroscann_ai.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Stripe;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Stripe if secret key is present in configuration
var _stripeSecret = builder.Configuration["PaymentGateway:Stripe:SecretKey"];
if (!string.IsNullOrEmpty(_stripeSecret))
{
    StripeConfiguration.ApiKey = _stripeSecret;
}

// Add services
builder.Services.AddControllersWithViews();

// Database Context
builder.Services.AddDbContext<ElectroscannDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("dbcs");
    // NOTE: currently unused — app runs on SQLite (electroscann.db) for local/demo reliability.
    if (string.IsNullOrEmpty(connectionString))
    {
        options.UseSqlite("Data Source=electroscann.db");
    }
    else
    {
        options.UseSqlite("Data Source=electroscann.db");
    }
});

// Services
builder.Services.AddScoped<INotificationService, NotificationService>();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        // Use SameAsRequest so the app works on both HTTP (dev) and HTTPS (prod)
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "ElectroScanAuth";
    });

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ElectricianOnly", policy => policy.RequireRole("Electrician"));
    options.AddPolicy("CompanyOnly", policy => policy.RequireRole("Company"));
    options.AddPolicy("ClientOnly", policy => policy.RequireRole("Client"));
});

// Password Hasher
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ElectroscannDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<User>>();
        await DbSeeder.SeedAsync(db, hasher);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Disabled in dev/preview for container reverse-proxy compatibility; re-enabled automatically outside Development.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseSession();       // Session must come BEFORE authentication
app.UseAuthentication();
app.UseAuthorization();

// Startup Configuration Warnings
var geminiKeyCheck = builder.Configuration["Gemini:ApiKey"] ?? builder.Configuration["GEMINI_API_KEY"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
if (string.IsNullOrEmpty(geminiKeyCheck))
{
    app.Logger.LogWarning("WARNING: Gemini API key (Gemini:ApiKey) is missing. AI Wire Scanner will fall back to Groq, then Rule-Based Demo Mode.");
}

var groqKeyCheck = builder.Configuration["Groq:ApiKey"] ?? builder.Configuration["GROQ_API_KEY"] ?? Environment.GetEnvironmentVariable("GROQ_API_KEY");
if (string.IsNullOrEmpty(groqKeyCheck))
{
    app.Logger.LogWarning("WARNING: Groq Vision API key (Groq:ApiKey) is missing. AI Wire Scanner will run in Rule-Based Demo Mode.");
}

var stripeKeyCheck = builder.Configuration["PaymentGateway:Stripe:SecretKey"];
if (string.IsNullOrEmpty(stripeKeyCheck))
{
    app.Logger.LogWarning("WARNING: Stripe Secret Key (PaymentGateway:Stripe:SecretKey) is missing. Payment checkout will notify users that payments are currently disabled.");
}

// Routes
app.MapControllerRoute(
    name: "dashboard",
    pattern: "Dashboard/{action=Index}/{id?}",
    defaults: new { controller = "Dashboard" });

app.MapControllerRoute(
    name: "account",
    pattern: "Account/{action=Index}/{id?}",
    defaults: new { controller = "Account", action = "Index" });

app.MapControllerRoute(
    name: "electrician",
    pattern: "Electrician/{action=Index}/{id?}",
    defaults: new { controller = "ElectricianDashboard" });

app.MapControllerRoute(
    name: "company",
    pattern: "Company/{action=Index}/{id?}",
    defaults: new { controller = "CompanyDashboard" });

app.MapControllerRoute(
    name: "client",
    pattern: "Client/{action=Index}/{id?}",
    defaults: new { controller = "ClientDashboard" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
