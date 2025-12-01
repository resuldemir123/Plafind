## PlaceFind – Alanya İşletme Rehberi

PlaceFind, Alanya’daki işletmeleri (restoran, otel, mağaza, spa, vs.) keşfetmek, filtrelemek, incelemek, yorumlamak ve favorilere eklemek için geliştirilmiş modern bir işletme rehberi uygulamasıdır.  
Kullanıcılar için zengin bir ön yüz; adminler için kapsamlı bir yönetim paneli sunar.

---

## Mimari Genel Bakış

- **Uygulama Türü**: ASP.NET Core 8.0 MVC (Server-side rendered)
- **Katmanlar** (tek proje içinde mantıksal ayrım):
  - `Data`       → EF Core + DbContext + Migration’lar + Seed işlemleri
  - `Models`     → Entity ve ViewModel sınıfları
  - `ViewModels` → Özelleşmiş sayfa modelleri (ör. detay sayfaları, profil sayfaları)
  - `Services`   → Email, SMS, Gemini (Yapay Zekâ) gibi dış servisler
  - `Options`    → Konfigürasyon seçenekleri (Gemini, Google Maps)
  - `Controllers`→ HTTP endpoint’leri (MVC Controller’lar)
  - `Views`      → Razor view’lar (UI katmanı)
- **Veritabanı**: SQL Server (EF Core Code-First + Migration)
- **Kimlik Doğrulama**: ASP.NET Core Identity (Email/şifre, opsiyonel Google OAuth)
- **Ön Yüz**: Bootstrap 5, Font Awesome, custom CSS (`site.css`)
- **Tema**: Dark / Light tema desteği (navbar’da toggle butonu)

---

## Klasör Yapısı

### `Controllers`

- `HomeController`
  - Ana sayfa, hakkında, iletişim, yardım, arama, gizlilik sayfaları
- `BusinessesController`
  - İşletme listeleme, detay, oluşturma/düzenleme (bazı kısmı admin tarafı ile entegre)
- `CategoriesController`
  - Kategori yönetimi (liste, oluşturma, düzenleme)
- `ReservationsController`
  - Rezervasyon oluşturma, kullanıcı ve admin tarafı listeleri (`Index`, `AdminIndex`)
- `ReviewsController`
  - Yorum ekleme ve listeleme
- `NewsController`
  - Haberler (liste, detay, CRUD)
- `AccountController`
  - Giriş, kayıt, şifre sıfırlama, profil, erişim engellendi vb.
- `UserController`
  - Kullanıcının kendi paneli, favoriler, yorumlar, profil düzenleme
- `UsersController`
  - Admin tarafında kullanıcı listesi, detayları
- `AdminController`
  - Admin dashboard, istatistikler, loglar, ayarlar, roller, izinler vb.
- `AiController`
  - Gemini tabanlı sohbet / öneri endpoint’i

### `Data`

- `ApplicationDbContext`
  - EF Core DbContext, tüm DbSet’ler:
    - `Business`, `Category`, `Review`, `Reservation`, `News`,
    - `UserProfile`, `UserFavorite`, `UserPhoto`, `AdminLog` vb.
  - İlişki ve fluent API konfigürasyonları
- `DbSeeder`
  - Örnek kategoriler, işletmeler ve haberler ile başlangıç verisi
- `IdentitySeeder`
  - Admin rolü ve admin kullanıcı oluşturma
- `Migrations`
  - Tüm veritabanı migration’ları + `ApplicationDbContextModelSnapshot`

### `Models`

**Entity Modelleri:**
- `ApplicationUser` (IdentityUser türevi, kullanıcı ek alanları)
- `Business` (işletme bilgileri: ad, adres, telefon, kategori, rating, vb.)
- `Category`
- `Review`
- `Reservation`
- `News`
- `UserProfile`, `UserPhoto`, `UserFavorite`
- `AdminLog`

**View Modeller:**
- `LoginViewModel`, `RegisterViewModel`, `ForgotPasswordViewModel`, `ResetPasswordViewModel`
- `ContactViewModel`, `EmailSettingsViewModel`, `SiteSettingsViewModel`
- `ChangePasswordViewModel` (Account ve Admin için versiyonlar)

### `ViewModels`

- `BusinessDetailsViewModel`
  - Detay sayfasında işletme + yorumlar + kullanıcı bilgisi
- `UserProfileViewModel`
  - Kullanıcı panelinde gerekli profil alanları
- `ChangeEmailViewModel`, `ChangePasswordViewModel`
- `GeminiChatRequest`
  - AI endpoint’ine gönderilen istek modeli

### `Services`

- `EmailService`
  - `EmailSettings` üzerinden SMTP (ör. Gmail) ile mail gönderimi
- `SmsService`
  - Basit SMS servis entegrasyonu (Netgsm/Twilio için yapı taşları)
- `GeminiChatService` / `IGeminiChatService`
  - Google Gemini API üzerinden öneri/cevap üretimi  
  - Model: `gemini-1.5-flash` (REST HTTP client)

### `Options`

- `GeminiOptions`
  - `GoogleGemini:ApiKey` konfigürasyonunu temsil eder
- `GoogleMapsOptions`
  - `GoogleMaps:ApiKey` harita API key’ini temsil eder

### `Views`

- `Views/Home`
  - `Index.cshtml`  → Hero, öne çıkan işletmeler, kategoriler, haberler, vb.
  - `Search.cshtml` → Gelişmiş arama ve filtre ekranı
  - `About`, `Contact`, `Help`, `Privacy`
- `Views/Businesses`
  - `Index`, `Details`, `Create`, `Map`
- `Views/Categories`
  - `Index`, `Create`
- `Views/Reviews`
  - `Index`, `Create`
- `Views/Reservations`
  - `Index`, `Create`, `Details`, `AdminIndex`
- `Views/News`
  - `Index`, `Details`, `Create`, `Edit`, `Delete`
- `Views/Account`
  - `Login`, `Register`, `ForgotPassword`, `ResetPassword`, `AccessDenied`, `Profile`
- `Views/User`
  - `Index` (dashboard), `Businesses`, `Favorites`, `MyReviews`, `Profile`, `BusinessDetails`
- `Views/Admin`
  - Dashboard ve tüm yönetim ekranları (`Users`, `Businesses`, `Categories`, `Logs`, `Settings`, `Roles`, `Permissions`, `Tickets`, vb.)
- `Views/Shared`
  - `_Layout.cshtml`        → Ana layout (navbar, footer, tema butonu)
  - `_AdminLayout.cshtml`   → Admin paneline özel layout
  - `_LoginPartial.cshtml`  → Kullanıcı menüsü (giriş/çıkış, profil)
  - `_ValidationScriptsPartial.cshtml`
  - `Error.cshtml`          → Genel hata sayfası

---

## Program Akışı (Program.cs)

- `ApplicationDbContext` SQL Server ile configure edilir (`DefaultConnection`).
- ASP.NET Identity ayarları yapılır (şifre gereksinimleri, cookie ayarları).
- Servisler:
  - `EmailService`, `SmsService`
  - `GeminiChatService` (HTTP client)
  - `GoogleMapsOptions`, `GeminiOptions`
- **Seed Data:**
  - Development ortamında (veya `SeedDataOnStartup == true` ise)  
    - Kategoriler, işletmeler, haberler eklenir  
    - Admin kullanıcı ve roller oluşturulur
- Pipeline:
  - Development: DeveloperExceptionPage
  - Production: `UseExceptionHandler("/Home/Error")`, HSTS, güvenlik header’ları
  - `UseHttpsRedirection`, `UseStaticFiles`
  - Routing + Authentication + Authorization
  - Varsayılan route: `{controller=Home}/{action=Index}/{id?}`

---

## Tema Sistemi (Dark / Light)

- HTML root’ta `data-theme="light"` / `data-theme="dark"` attribute’u kullanılır.
- `wwwroot/css/site.css` içinde CSS değişkenleri (`--bg-primary`, `--text-primary`, vb.)
- Navbar’da bulunan buton:
  - `wwwroot/js/theme-toggle.js` ile:
    - LocalStorage’da tema kaydı
    - Sayfa yüklenirken son seçilen tema geri yüklenir
    - Ay / Güneş ikonları arasında geçiş

---

## Konfigürasyon (appsettings)

### `appsettings.json` (Development)

- `ConnectionStrings:DefaultConnection` → LocalDB veya geliştirme SQL Server
- `EmailSettings` → SMTP bilgileri (development için test değerleri)
- `Authentication:Google` → Dev ortamı için Google OAuth (boş da olabilir)
- `GoogleGemini:ApiKey`, `GoogleMaps:ApiKey` → Geliştirme anahtarları (boş olabilir)

### `appsettings.Production.json` (Şablon)

Bu dosya için bir şablon var; prod değerlerini **sen doldurmalısın**:

- Prod SQL Server bağlantı bilgisi
- Gerçek SMTP şifresi
- Production Google OAuth ClientId/Secret
- Production Gemini ve Google Maps API anahtarları
- `AllowedHosts` → domain adların

Hassas bilgileri Git’e koymak yerine environment variable ile override etmek önerilir.

---

## Çalıştırma (Development)

cd Plafind/Plafind

# Migration (ilk sefer)
dotnet ef database update

# Uygulamayı çalıştır
dotnet run- Varsayılan URL: `https://localhost:5001` veya `http://localhost:5000`
- Admin kullanıcı: Seed içinde tanımlanmıştır (kendi şifreni `IdentitySeeder` içinde veya DB’de güncelleyebilirsin).

---

## Yayınlama (Özet – IIS)

1. Publish:
  
   dotnet publish -c Release -o ./publish
   2. Production veritabanını oluştur, connection string’i ayarla.
3. `dotnet ef database update --environment Production` ile migration çalıştır.
4. IIS’te:
   - Application Pool (No Managed Code)
   - Site (physical path: `publish` klasörü)
5. Ortam değişkeni:
   - `ASPNETCORE_ENVIRONMENT=Production`
6. Siteyi başlat ve test et.

---

## Güvenlik Notları

- `appsettings.Production.json` ve tüm şifreler **Git’e commit edilmemelidir**.
- Production ortamda:
  - HTTPS zorunlu,
  - Güçlü admin şifresi,
  - Düzenli veritabanı backup’ı önerilir.
- AI, Maps ve Google OAuth anahtarları environment variable veya güvenli secret store üzerinden yönetilmelidir.

---

## İletişim / Geliştirme

- Proje sahibi: `resuldemir123`
- GitHub: `https://github.com/resuldemir123/Plafind`
- Canlı ortam: (kurulum sonrası kendi domain’inizi buraya ekleyebilirsiniz)

Bu README, projeyi yeni gören bir geliştiricinin hem kod yapısını, hem de nasıl ayağa kaldıracağını tek dosyada anlayabilmesi için hazırlanmıştır.
