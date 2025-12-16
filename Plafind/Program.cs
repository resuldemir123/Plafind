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
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// DATABASE
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseProvider = builder.Configuration["Database:Provider"] ?? "MySQL"; // MySQL veya SqlServer

// Connection string'e göre veritabanı tipini belirle
var isMySql = databaseProvider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) ||
              (!string.IsNullOrEmpty(connectionString) && 
               (connectionString.Contains("Port=") || 
                connectionString.Contains("User=") || 
                connectionString.Contains("CharSet=")) &&
               !connectionString.Contains("MSSQLLocalDB") &&
               !connectionString.Contains("Trusted_Connection") &&
               !connectionString.Contains("Integrated Security"));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (isMySql)
    {
        // MySQL bağlantısı
        // ServerVersion otomatik algılama yerine manuel versiyon belirtiyoruz
        var serverVersion = ServerVersion.Parse("8.0.21-mysql"); // MySQL 8.0.21 veya üzeri
        
        options.UseMySql(connectionString, serverVersion, mySqlOptions =>
        {
            mySqlOptions.SchemaBehavior(MySqlSchemaBehavior.Ignore);
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            mySqlOptions.MigrationsAssembly("Plafind");
        });
    }
    else
    {
        // SQL Server bağlantısı (fallback)
        options.UseSqlServer(connectionString);
    }
});

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

// SESSION
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// LOCALIZATION (Çoklu Dil Desteği)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// MVC - View Localization desteği ile
builder.Services.AddControllersWithViews()
    .AddViewLocalization();

// RAZOR PAGES
builder.Services.AddRazorPages();

// Desteklenen dilleri tanımla
const string defaultCulture = "tr-TR";
var supportedCultures = new[]
{
    new CultureInfo("tr-TR"), // Türkçe
    new CultureInfo("en-US"), // İngilizce
};

// Yerelleştirme Middleware'ini yapılandır
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(defaultCulture);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider()); // Çerezden okuma (Dil Seçimi için)
});

// EMAIL SERVICE
builder.Services.AddScoped<IEmailService, EmailService>();

// SMS SERVICE
builder.Services.AddScoped<ISmsService, SmsService>();

// NOTIFICATION SERVICE
builder.Services.AddScoped<INotificationService, NotificationService>();

// COMPARISON SERVICE
builder.Services.AddScoped<IComparisonService, ComparisonService>();

// COMPARE SERVICE (Session-based comparison)
builder.Services.AddScoped<ICompareService, CompareService>();

// GOOGLE NEWS SERVICE - Timeout ve retry ayarları ile
builder.Services.AddHttpClient<IGoogleNewsService, GoogleNewsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30); // 30 saniye timeout
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
});

// BACKGROUND SERVICE - Turizm haberlerini otomatik senkronize et
builder.Services.AddHostedService<TourismNewsBackgroundService>();

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("GoogleGemini"));
builder.Services.AddHttpClient<IGeminiChatService, GeminiChatService>();
builder.Services.Configure<GoogleMapsOptions>(builder.Configuration.GetSection("GoogleMaps"));
builder.Services.Configure<TomTomOptions>(builder.Configuration.GetSection("TomTom"));

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
            // Veritabanının hazır olduğundan emin ol
            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();
            
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            
            await Plafind.Data.DbSeeder.SeedDataAsync(context);
            await Plafind.Data.IdentitySeeder.SeedAdminAsync(userManager, roleManager);
            await Plafind.Data.BusinessOwnerSeeder.SeedBusinessOwnerAsync(context, userManager, roleManager);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Seed data oluşturulurken bir hata oluştu: {Message}", ex.Message);
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

app.UseSession();

// LOCALIZATION MIDDLEWARE (Routing'den önce olmalı)
var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();