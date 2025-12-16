# Production Deployment Kontrol Listesi ✅

## 🔒 Güvenlik Ayarları

### ✅ Tamamlananlar
- [x] Production için güvenlik header'ları eklendi
- [x] HTTPS yönlendirmesi yapılandırıldı
- [x] Cookie güvenliği production için ayarlandı
- [x] HSTS (HTTP Strict Transport Security) eklendi
- [x] .gitignore dosyası oluşturuldu (hassas veriler korunuyor)
- [x] appsettings.Production.json şablonu oluşturuldu

### ⚠️ Yapılması Gerekenler

1. **appsettings.Production.json Dosyasını Doldurun**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Production veritabanı bağlantı string'i"
     },
     "EmailSettings": {
       "SmtpPassword": "Production email şifresi"
     },
     "Authentication": {
       "Google": {
         "ClientId": "Production Google Client ID",
         "ClientSecret": "Production Google Client Secret"
       }
     },
     "GoogleGemini": {
       "ApiKey": "Production Gemini API Key"
     },
     "GoogleMaps": {
       "ApiKey": "Production Google Maps API Key"
     },
     "AllowedHosts": "yourdomain.com,www.yourdomain.com"
   }
   ```

2. **Environment Variables (Önerilen)**
   - Hassas bilgileri environment variables olarak ayarlayın
   - Windows: System Properties > Environment Variables
   - Linux: `/etc/environment` veya `.env` dosyası

3. **Veritabanı**
   - [ ] Production SQL Server kurulumu
   - [ ] Connection string test edildi
   - [ ] Migration'lar uygulandı: `dotnet ef database update --environment Production`
   - [ ] Backup stratejisi oluşturuldu

4. **SSL Sertifikası**
   - [ ] HTTPS sertifikası yüklendi
   - [ ] SSL sertifikası geçerli ve güncel

5. **API Anahtarları**
   - [ ] Google Maps API Key (Production)
   - [ ] Google Gemini API Key (Production)
   - [ ] Google OAuth Client ID/Secret (Production callback URL ile)

## 📦 Deployment Adımları

### IIS Deployment
1. Release build oluşturun:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. IIS'te:
   - Application Pool oluşturun (.NET CLR Version: No Managed Code)
   - Web Site oluşturun
   - Publish klasöründeki dosyaları kopyalayın
   - HTTPS binding ekleyin

### Azure App Service
1. Azure Portal'da Web App oluşturun
2. Deployment Center'dan GitHub/Azure DevOps bağlayın
3. Application Settings'te environment variables ekleyin
4. Connection String'i yapılandırın

### Docker
```bash
docker-compose build
docker-compose up -d
```

## 🔍 Son Kontroller

- [ ] Tüm API anahtarları production için yapılandırıldı
- [ ] Veritabanı bağlantısı test edildi
- [ ] HTTPS çalışıyor
- [ ] Static files yükleniyor
- [ ] Email gönderimi test edildi
- [ ] Google OAuth çalışıyor
- [ ] Error logging çalışıyor
- [ ] Backup stratejisi aktif

## 📝 Önemli Notlar

⚠️ **GÜVENLİK UYARILARI:**
- `appsettings.Production.json` dosyasını ASLA Git'e commit etmeyin
- Tüm şifreleri ve API anahtarlarını environment variables olarak kullanın
- Production'da seed data otomatik çalışmaz (güvenlik için)
- Admin şifresini production'da değiştirin

## 🚀 Hızlı Başlangıç

1. `appsettings.Production.json` dosyasını doldurun
2. Environment variables ayarlayın
3. Release build oluşturun: `dotnet publish -c Release`
4. Veritabanı migration'larını uygulayın
5. Dosyaları sunucuya kopyalayın
6. IIS/Azure/Docker'da yapılandırın
7. Test edin!

## 📞 Destek

Sorun yaşarsanız `DEPLOYMENT.md` dosyasına bakın veya log dosyalarını kontrol edin.

