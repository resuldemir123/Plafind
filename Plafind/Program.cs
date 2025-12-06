using Plafind.Data;
using Plafind.Models;
using Plafind.Options;
using Plafind.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

// DATABASE
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// IDENTITY
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// COOKIE AYARLARI
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
        ? CookieSecurePolicy.Always 
        : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// MVC
builder.Services.AddControllersWithViews();

// RAZOR PAGES
builder.Services.AddRazorPages();

// EMAIL SERVICE
builder.Services.AddScoped<IEmailService, EmailService>();

// SMS SERVICE
builder.Services.AddScoped<ISmsService, SmsService>();

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("GoogleGemini"));
builder.Services.AddHttpClient<IGeminiChatService, GeminiChatService>();
builder.Services.Configure<GoogleMapsOptions>(builder.Configuration.GetSection("GoogleMaps"));

// MEMORY CACHE (SMS kodları için)
builder.Services.AddMemoryCache();

// GOOGLE AUTHENTICATION
// Sadece ClientId ve ClientSecret doluysa Google auth ekle
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.CallbackPath = "/signin-google"; // Varsayılan path
        });
}

var app = builder.Build();

// Seed data - Sadece Development'ta veya ilk kurulumda
if (app.Environment.IsDevelopment() || 
    builder.Configuration.GetValue<bool>("SeedDataOnStartup", false))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await Plafind.Data.DbSeeder.SeedDataAsync(context);
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            await Plafind.Data.IdentitySeeder.SeedAdminAsync(userManager, roleManager);
            await Plafind.Data.BusinessOwnerSeeder.SeedBusinessOwnerAsync(context, userManager, roleManager);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Seed data oluşturulurken bir hata oluştu.");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    
    // Production Security Headers
    app.Use(async (context, next) =>
    {
        if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
            context.Response.Headers.Append("X-Frame-Options", "DENY");
        if (!context.Response.Headers.ContainsKey("X-XSS-Protection"))
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        if (!context.Response.Headers.ContainsKey("Referrer-Policy"))
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        if (!context.Response.Headers.ContainsKey("Permissions-Policy"))
            context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
        
        if (context.Request.IsHttps && !context.Response.Headers.ContainsKey("Strict-Transport-Security"))
        {
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        }
        
        await next();
    });
}

// HTTPS Redirection - Production'da zorunlu
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "images")),
    RequestPath = "/images"
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();