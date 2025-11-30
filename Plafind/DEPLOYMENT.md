# Deployment Rehberi

## Production Hazırlık Kontrol Listesi

### ✅ Tamamlanması Gerekenler

1. **Veritabanı**
   - [ ] Production SQL Server kurulumu
   - [ ] Connection string yapılandırması
   - [ ] Migration'ların uygulanması
   - [ ] Backup stratejisi

2. **API Anahtarları**
   - [ ] Google Maps API Key
   - [ ] Google Gemini API Key
   - [ ] Google OAuth Client ID ve Secret
   - [ ] SMTP Email ayarları

3. **Güvenlik**
   - [ ] HTTPS sertifikası
   - [ ] Güçlü admin şifresi
   - [ ] Environment variables yapılandırması
   - [ ] appsettings.Production.json güvenliği

4. **Performans**
   - [ ] CDN yapılandırması (opsiyonel)
   - [ ] Static files caching
   - [ ] Database indexing

5. **Monitoring**
   - [ ] Logging yapılandırması
   - [ ] Error tracking (opsiyonel)
   - [ ] Performance monitoring

## Hızlı Başlangıç

### 1. Environment Variables Ayarlama

Production sunucuda environment variables olarak ayarlayın:

```bash
# Windows (IIS)
[Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=...", "Machine")

# Linux
export ConnectionStrings__DefaultConnection="Server=..."
export EmailSettings__SmtpPassword="..."
export Authentication__Google__ClientSecret="..."
```

### 2. Publish İşlemi

```bash
dotnet publish -c Release -o ./publish
```

### 3. Dosyaları Sunucuya Kopyalama

Publish klasöründeki tüm dosyaları production sunucusuna kopyalayın.

### 4. Veritabanı Migration

```bash
dotnet ef database update --environment Production
```

### 5. IIS Yapılandırması

1. Application Pool oluşturun (.NET CLR Version: No Managed Code)
2. Web Site oluşturun
3. Physical path'i publish klasörüne ayarlayın
4. Binding'de HTTPS ekleyin

## Docker ile Deployment

```bash
# Build
docker-compose build

# Run
docker-compose up -d

# Logs
docker-compose logs -f
```

## Azure App Service Deployment

1. Azure Portal'da Web App oluşturun
2. Deployment Center'dan GitHub/Azure DevOps bağlayın
3. Application Settings'te environment variables ekleyin
4. Connection String'i yapılandırın

## Troubleshooting

### Veritabanı Bağlantı Hatası
- Connection string'i kontrol edin
- Firewall ayarlarını kontrol edin
- SQL Server'ın çalıştığından emin olun

### Static Files Yüklenmiyor
- wwwroot klasörünün kopyalandığından emin olun
- IIS'te static file handler'ın aktif olduğundan emin olun

### HTTPS Çalışmıyor
- SSL sertifikasının yüklü olduğundan emin olun
- Binding'de HTTPS port'unun doğru olduğundan emin olun

