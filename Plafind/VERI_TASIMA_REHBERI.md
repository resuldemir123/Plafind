# Veri Taşıma Rehberi - SQL Server'dan MySQL'e

Migration sadece şemayı oluşturur, verileri taşımaz. Verileri manuel olarak taşımanız gerekir.

## ⚠️ ÖNEMLİ: Yedek Alın!

Veri taşıma işleminden önce mutlaka SQL Server veritabanının yedeğini alın!

## Yöntem 1: MySQL Workbench Migration Wizard (ÖNERİLEN - EN KOLAY)

Bu yöntem en güvenli ve otomatik yöntemdir.

### Adımlar:

1. **MySQL Workbench'i açın**

2. **Database > Migrate Database** menüsüne gidin

3. **Source Database (SQL Server) Ayarları:**
   - **Connection Method:** `ODBC` veya `Connection String`
   - **ODBC DSN:** SQL Server için bir DSN oluşturun (Windows'ta ODBC Data Source Administrator'dan)
   - **Veya Connection String:**
     ```
     Driver={ODBC Driver 17 for SQL Server};Server=(localdb)\MSSQLLocalDB;Database=AlanyaBusinessGuide;Trusted_Connection=yes;
     ```

4. **Target Database (MySQL) Ayarları:**
   - **Hostname:** `localhost`
   - **Port:** `3306`
   - **Username:** `root`
   - **Password:** MySQL şifreniz
   - **Default Schema:** `AlanyaBusinessGuide`

5. **Migration Ayarları:**
   - **Schema Mapping:** Tabloları kontrol edin
   - **Data Type Mapping:** Veri tiplerini gözden geçirin
   - **Migration Options:** 
     - ✅ Create target schema
     - ✅ Migrate table data
     - ✅ Migrate stored procedures (varsa)

6. **Migration'ı Başlatın:**
   - **Start Migration** butonuna tıklayın
   - İşlem tamamlanana kadar bekleyin
   - Hataları kontrol edin

7. **Doğrulama:**
   - Veri sayılarını kontrol edin (`VERI_KONTROL_REHBERI.md` dosyasına bakın)

## Yöntem 2: SQL Server Management Studio + MySQL Workbench (Manuel)

### Adım 1: SQL Server'dan Veri Export

1. **SQL Server Management Studio**'yu açın
2. `AlanyaBusinessGuide` veritabanına bağlanın
3. **Tasks > Export Data** seçeneğini kullanın
4. **Export Wizard**'ı takip edin:
   - Source: SQL Server
   - Destination: Flat File (CSV) veya SQL Script
   - Tabloları seçin
   - Export'u tamamlayın

### Adım 2: MySQL'e Veri Import

1. **MySQL Workbench**'i açın
2. `AlanyaBusinessGuide` veritabanına bağlanın
3. **Server > Data Import** menüsüne gidin
4. Export edilen dosyaları seçin
5. Import'u başlatın

## Yöntem 3: SQL Script ile Manuel Taşıma

### Adım 1: SQL Server'dan INSERT Scriptleri Oluştur

**SQL Server Management Studio'da:**

```sql
-- Her tablo için INSERT scriptleri oluşturun
-- Örnek: Categories tablosu için

SELECT 
    'INSERT INTO Categories (Id, Name, Description, CreatedDate) VALUES (' +
    CAST(Id AS VARCHAR) + ', ' +
    '''' + REPLACE(Name, '''', '''''') + ''', ' +
    ISNULL('''' + REPLACE(Description, '''', '''''') + '''', 'NULL') + ', ' +
    '''' + CONVERT(VARCHAR, CreatedDate, 120) + ''');'
FROM Categories;
```

Bu sorguyu her tablo için çalıştırın ve sonuçları kopyalayın.

### Adım 2: MySQL'de Scriptleri Çalıştır

1. MySQL Workbench'te yeni bir SQL sorgusu açın
2. INSERT scriptlerini yapıştırın
3. **Sıraya dikkat edin:** Foreign key bağımlılıklarına göre:
   - Önce: Categories, AspNetRoles
   - Sonra: AspNetUsers
   - Sonra: AspNetUserRoles, Businesses
   - En son: Diğer tablolar

## Yöntem 4: C# Programatik Taşıma (Gelişmiş)

Eğer programatik olarak taşımak isterseniz, `Scripts/MigrateToMySQL.cs` dosyasını kullanabilirsiniz (ancak bu dosya için ek paketler gerekebilir).

## Önemli Notlar

### Foreign Key Sırası

Verileri taşırken şu sırayı takip edin:

1. **AspNetRoles** (Roller)
2. **Categories** (Kategoriler)
3. **AspNetUsers** (Kullanıcılar)
4. **AspNetUserRoles** (Kullanıcı-Rol ilişkileri)
5. **Businesses** (İşletmeler)
6. **UserProfiles** (Kullanıcı Profilleri)
7. **Reviews** (Yorumlar)
8. **Reservations** (Rezervasyonlar)
9. **News** (Haberler)
10. **UserFavorites** (Favoriler)
11. **Diğer tablolar**

### Veri Tipi Dönüşümleri

SQL Server ve MySQL arasında bazı veri tipleri farklıdır:

| SQL Server | MySQL |
|-----------|-------|
| `nvarchar(max)` | `longtext` |
| `nvarchar(n)` | `varchar(n)` |
| `datetime2` | `datetime` |
| `bit` | `tinyint(1)` |

MySQL Workbench Migration Wizard bu dönüşümleri otomatik yapar.

### Identity/Auto-Increment

SQL Server'daki Identity kolonları MySQL'de Auto-Increment olarak taşınır. Ancak mevcut ID değerlerini korumak için:

```sql
-- MySQL'de
SET FOREIGN_KEY_CHECKS = 0;
-- INSERT komutları
SET FOREIGN_KEY_CHECKS = 1;

-- Auto-increment'i ayarla
ALTER TABLE TableName AUTO_INCREMENT = (MAX(Id) + 1);
```

## Sorun Giderme

### "Foreign key constraint fails"

**Çözüm:** Verileri doğru sırayla taşıdığınızdan emin olun (yukarıdaki sıraya bakın).

### "Duplicate entry for key 'PRIMARY'"

**Çözüm:** Tabloyu temizleyin veya INSERT IGNORE kullanın:
```sql
INSERT IGNORE INTO TableName (...) VALUES (...);
```

### "Incorrect string value"

**Çözüm:** Connection string'e `CharSet=utf8mb4;` ekleyin ve veritabanı charset'ini kontrol edin:
```sql
ALTER DATABASE AlanyaBusinessGuide CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

## Hızlı Başlangıç (Özet)

1. ✅ MySQL Workbench'i açın
2. ✅ Database > Migrate Database
3. ✅ SQL Server'ı source, MySQL'i target olarak seçin
4. ✅ Migration'ı başlatın
5. ✅ Veri sayılarını kontrol edin

## Sonraki Adım

Veri taşıma işlemi tamamlandıktan sonra `VERI_KONTROL_REHBERI.md` dosyasındaki talimatları takip ederek verilerin doğru taşındığını kontrol edin.
