# PlaceFind - Alanya İşletme Rehberi

PlaceFind, Alanya'daki işletmeleri keşfetmek, yorum yapmak ve favorilere eklemek için modern bir web platformudur.

## Özellikler

- 🏢 İşletme listeleme ve arama
- ⭐ Yorum ve puanlama sistemi
- ❤️ Favori işletmeler
- 📅 Rezervasyon sistemi
- 📰 Haberler ve duyurular
- 🗺️ Harita entegrasyonu
- 🌙 Dark/Light tema desteği
- 🔐 Kullanıcı kimlik doğrulama (Email, Google)
- 👤 Kullanıcı ve Admin panelleri
- 🤖 AI destekli arama (Gemini)

## Teknolojiler

- **Backend**: ASP.NET Core 8.0 MVC
- **Database**: SQL Server
- **Authentication**: ASP.NET Core Identity
- **Frontend**: Bootstrap 5, Font Awesome
- **Maps**: Google Maps API
- **AI**: Google Gemini API

## Kurulum

### Gereksinimler

- .NET 8.0 SDK
- SQL Server (LocalDB veya SQL Server)
- Visual Studio 2022 veya VS Code

### Adımlar

1. **Projeyi klonlayın**
   ```bash
   git clone <repository-url>
   cd Plafind
   ```

2. **Veritabanını yapılandırın**
   - `appsettings.json` dosyasındaki `ConnectionStrings:DefaultConnection` değerini güncelleyin
   - Migration'ları uygulayın:
   ```bash
   dotnet ef database update
   ```

3. **API Anahtarlarını Yapılandırın**
   - `appsettings.json` dosyasında:
     - `GoogleGemini:ApiKey` - Gemini API anahtarı
     - `GoogleMaps:ApiKey` - Google Maps API anahtarı
     - `Authentication:Google:ClientId` ve `ClientSecret` - Google OAuth

4. **Uygulamayı çalıştırın**
   ```bash
   dotnet run
   ```

## Production Deployment

### 1. Production Ayarları

`appsettings.Production.json` dosyasını oluşturun ve aşağıdaki bilgileri güncelleyin:

- **ConnectionStrings**: Production veritabanı bağlantı bilgileri
- **EmailSettings**: SMTP sunucu bilgileri
- **Authentication:Google**: Production Google OAuth bilgileri
- **AllowedHosts**: Domain adınız

### 2. Environment Variables (Önerilen)

Hassas bilgileri environment variables olarak ayarlayın:

```bash
export ConnectionStrings__DefaultConnection="Server=..."
export EmailSettings__SmtpPassword="..."
export Authentication__Google__ClientSecret="..."
```

### 3. Build ve Publish

```bash
# Release modunda build
dotnet build -c Release

# Publish
dotnet publish -c Release -o ./publish
```

### 4. IIS Deployment

1. IIS'te yeni bir Application Pool oluşturun (.NET CLR Version: No Managed Code)
2. Yeni bir Web Site oluşturun
3. Publish klasöründeki dosyaları site klasörüne kopyalayın
4. Application Pool'u başlatın

### 5. Azure Deployment

```bash
# Azure'a publish
az webapp deployment source config-zip \
  --resource-group <resource-group> \
  --name <app-name> \
  --src ./publish.zip
```

### 6. Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Plafind.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Plafind.dll"]
```

## Güvenlik Notları

⚠️ **ÖNEMLİ**: Production'a geçmeden önce:

1. `appsettings.Production.json` dosyasını `.gitignore`'a ekleyin
2. Tüm API anahtarlarını ve şifreleri environment variables olarak ayarlayın
3. HTTPS kullanın
4. Güçlü şifreler kullanın
5. Database backup stratejisi oluşturun
6. Logging yapılandırmasını kontrol edin

## Lisans

Bu proje özel bir projedir.

## İletişim

- Email: info@plafind.com
- Website: https://plafind.com

