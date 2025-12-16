# SQL Server'dan MySQL'e Veri Taşıma Rehberi

Bu rehber, LocalDB'deki SQL Server veritabanını MySQL'e taşımanız için adım adım talimatlar içerir.

## Ön Hazırlık

### 1. MySQL Kurulumu
- MySQL Server 8.0 veya üzeri kurulu olmalı
- MySQL Workbench kurulu olmalı (önerilir)
- MySQL bağlantı bilgileriniz hazır olmalı

### 2. Proje Yapılandırması

#### appsettings.json Güncellemesi
```json
{
  "Database": {
    "Provider": "MySQL"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AlanyaBusinessGuide;User=root;Password=your_password;Port=3306;CharSet=utf8mb4;",
    "SqlServerConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AlanyaBusinessGuide;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

**ÖNEMLİ:** `your_password` kısmını gerçek MySQL şifrenizle değiştirin!

## Yöntem 1: MySQL Workbench Migration Wizard (ÖNERİLEN)

Bu yöntem en güvenli ve kullanıcı dostu yöntemdir.

### Adımlar:

1. **MySQL Workbench'i açın**

2. **Database > Migrate Database** menüsüne gidin

3. **Source Database** olarak SQL Server'ı seçin:
   - Connection Method: `ODBC`
   - System DSN: SQL Server için bir DSN oluşturun
   - Veya Connection String kullanın

4. **Target Database** olarak MySQL'i seçin:
   - Hostname: `localhost`
   - Port: `3306`
   - Username: `root`
   - Password: MySQL şifreniz
   - Default Schema: `AlanyaBusinessGuide`

5. **Migration Wizard'ı takip edin:**
   - Schema mapping'i kontrol edin
   - Veri tiplerini gözden geçirin
   - Migration'ı başlatın

6. **Migration sonrası kontrol:**
   - Tabloların oluşturulduğunu kontrol edin
   - Veri sayılarını karşılaştırın
   - Foreign key'leri kontrol edin

## Yöntem 2: Entity Framework Migrations

### Adımlar:

1. **Mevcut migration'ları temizleyin (isteğe bağlı):**
```bash
dotnet ef migrations remove
```

2. **MySQL için yeni migration oluşturun:**
```bash
dotnet ef migrations add InitialMySQLMigration
```

3. **MySQL veritabanını oluşturun:**
```bash
dotnet ef database update
```

4. **Verileri manuel olarak taşıyın** (aşağıdaki SQL scriptlerini kullanın)

## Yöntem 3: SQL Script ile Manuel Taşıma

### Adım 1: SQL Server'dan Veri Export

SQL Server Management Studio'da:

```sql
-- Tüm tabloları listeleyin
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- Her tablo için CSV export yapın veya INSERT scriptleri oluşturun
```

### Adım 2: MySQL'e Veri Import

MySQL Workbench'te veya mysql komut satırında:

```sql
-- Önce şemayı oluşturun (EF Core migrations ile)
-- Sonra verileri import edin

-- Örnek: Categories tablosu için
LOAD DATA LOCAL INFILE 'categories.csv'
INTO TABLE Categories
FIELDS TERMINATED BY ','
ENCLOSED BY '"'
LINES TERMINATED BY '\n'
IGNORE 1 ROWS;
```

## Yöntem 4: C# Migration Script (Gelişmiş)

`Scripts/MigrateToMySQL.cs` dosyasını kullanarak programatik taşıma yapabilirsiniz.

### Kullanım:

1. **appsettings.json'da her iki connection string'i ayarlayın**

2. **Script'i çalıştırın:**
```bash
dotnet run --project Plafind -- migrate-to-mysql
```

**NOT:** Bu script henüz tam entegre değilse, Program.cs'e eklemeniz gerekebilir.

## Önemli Notlar

### Veri Tipi Dönüşümleri

SQL Server ve MySQL arasında bazı veri tipleri farklıdır:

| SQL Server | MySQL |
|-----------|-------|
| `NVARCHAR(MAX)` | `TEXT` veya `LONGTEXT` |
| `DATETIME2` | `DATETIME` |
| `BIT` | `BOOLEAN` veya `TINYINT(1)` |
| `UNIQUEIDENTIFIER` | `CHAR(36)` |
| `VARBINARY(MAX)` | `BLOB` veya `LONGBLOB` |

### Identity Tabloları

ASP.NET Identity tabloları (`AspNetUsers`, `AspNetRoles`, vb.) özel dikkat gerektirir:

- Foreign key sıralamasına dikkat edin
- Identity seed değerlerini kontrol edin
- Password hash'lerin doğru taşındığından emin olun

### Foreign Key Sıralaması

Verileri taşırken foreign key bağımlılıklarına dikkat edin:

1. `AspNetRoles`
2. `Categories`
3. `AspNetUsers`
4. `Businesses`
5. `AspNetUserRoles`, `AspNetUserClaims`, vb.
6. Diğer tablolar...

## Doğrulama

Migration sonrası kontrol listesi:

- [ ] Tüm tablolar oluşturuldu mu?
- [ ] Veri sayıları eşleşiyor mu?
- [ ] Foreign key'ler doğru çalışıyor mu?
- [ ] Identity tabloları doğru çalışıyor mu?
- [ ] Uygulama başarıyla bağlanıyor mu?
- [ ] Login işlemleri çalışıyor mu?
- [ ] CRUD işlemleri test edildi mi?

## Sorun Giderme

### Bağlantı Hatası
```
Unable to connect to any of the specified MySQL hosts
```
**Çözüm:** MySQL servisinin çalıştığından ve port 3306'ın açık olduğundan emin olun.

### Character Set Hatası
```
Incorrect string value
```
**Çözüm:** Connection string'e `CharSet=utf8mb4;` ekleyin.

### Foreign Key Hatası
```
Cannot add or update a child row: a foreign key constraint fails
```
**Çözüm:** Verileri doğru sırayla taşıdığınızdan emin olun.

### Migration Hatası
```
Table 'xxx' already exists
```
**Çözüm:** Mevcut tabloları temizleyin veya `--force` parametresi kullanın.

## İletişim

Sorun yaşarsanız, migration loglarını kontrol edin ve gerekirse rollback yapın.
