# MySQL'e Hızlı Geçiş Rehberi

## Hızlı Adımlar

### 1. MySQL Bağlantı Bilgilerini Güncelle

`appsettings.json` dosyasını açın ve MySQL bağlantı bilgilerinizi girin:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AlanyaBusinessGuide;User=root;Password=SIFRENIZ_BURAYA;Port=3306;CharSet=utf8mb4;"
  }
}
```

### 2. NuGet Paketlerini Yükle

```bash
dotnet restore
```

### 3. MySQL Veritabanını Oluştur

MySQL'de veritabanını oluşturun:

```sql
CREATE DATABASE AlanyaBusinessGuide CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### 4. Migration'ları Uygula

```bash
dotnet ef database update
```

Bu komut:
- Tüm tabloları MySQL'de oluşturur
- Şemayı hazırlar

### 5. Verileri Taşı

**Seçenek A: MySQL Workbench Migration Wizard (ÖNERİLEN)**
1. MySQL Workbench'i açın
2. Database > Migrate Database
3. SQL Server'ı source, MySQL'i target olarak seçin
4. Wizard'ı takip edin

**Seçenek B: Manuel SQL Export/Import**
1. SQL Server Management Studio'da verileri export edin
2. MySQL Workbench'te import edin

**Seçenek C: Programatik Taşıma**
- `MIGRATION_GUIDE.md` dosyasındaki detaylı talimatları takip edin

### 6. Uygulamayı Test Et

```bash
dotnet run
```

Uygulamanın MySQL'e bağlandığını ve çalıştığını kontrol edin.

## Önemli Notlar

⚠️ **Yedek Alın:** Migration öncesi mutlaka SQL Server veritabanınızın yedeğini alın!

⚠️ **Şifre Güvenliği:** `appsettings.json` dosyasını Git'e commit etmeden önce şifreleri kaldırın veya User Secrets kullanın.

⚠️ **Test Ortamında Deneyin:** Önce test ortamında deneyin, production'a geçmeden önce.

## Sorun Giderme

### "Unable to connect to MySQL server"
- MySQL servisinin çalıştığından emin olun
- Port 3306'ın açık olduğunu kontrol edin
- Kullanıcı adı ve şifrenin doğru olduğunu kontrol edin

### "Table already exists"
- Mevcut tabloları silin veya farklı bir veritabanı adı kullanın

### "Character set hatası"
- Connection string'de `CharSet=utf8mb4;` olduğundan emin olun
- Veritabanı `utf8mb4_unicode_ci` collation kullanmalı

## Geri Dönüş

Eğer MySQL'e geçişte sorun yaşarsanız:

1. `appsettings.json`'da `Database:Provider` değerini `SqlServer` yapın
2. Connection string'i SQL Server'a geri çevirin
3. Uygulamayı yeniden başlatın
