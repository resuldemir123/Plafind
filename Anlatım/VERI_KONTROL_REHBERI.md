# Veri Kontrol Rehberi

SQL Server'dan MySQL'e veri taşıma işleminden sonra verilerin doğru taşındığını kontrol etmek için bu rehberi kullanın.

## Yöntem 1: SQL Sorguları ile Kontrol (ÖNERİLEN)

### Adım 1: SQL Server'daki Veri Sayılarını Kontrol Edin

1. **SQL Server Management Studio**'yu açın
2. `AlanyaBusinessGuide` veritabanına bağlanın
3. **New Query** ile yeni bir sorgu penceresi açın
4. `Scripts/CheckDataCounts.sql` dosyasındaki sorguyu çalıştırın

Bu sorgu tüm tablolardaki kayıt sayılarını gösterecek.

**Örnek Çıktı:**
```
TableName          RecordCount
-----------------  -----------
AspNetUsers        25
AspNetRoles        3
Businesses         150
Categories         12
Reviews            450
...
```

### Adım 2: MySQL'deki Veri Sayılarını Kontrol Edin

1. **MySQL Workbench**'i açın
2. `AlanyaBusinessGuide` veritabanına bağlanın
3. Yeni bir SQL sorgusu açın
4. `Scripts/CheckMySQLDataCounts.sql` dosyasındaki sorguyu çalıştırın

### Adım 3: Karşılaştırma Yapın

Her iki veritabanındaki kayıt sayılarını karşılaştırın. Sayılar eşleşiyorsa veriler başarıyla taşınmış demektir.

## Yöntem 2: Uygulama Üzerinden Kontrol

### Kullanıcılar
- Uygulamada login olmayı deneyin
- Kullanıcı listesini kontrol edin
- Profil bilgilerini kontrol edin

### İşletmeler
- İşletme listesini kontrol edin
- İşletme detaylarını açın
- İşletme görsellerini kontrol edin

### Yorumlar
- Yorumları listeleyin
- Yorum içeriklerini kontrol edin
- Yorum yanıtlarını kontrol edin

### Rezervasyonlar
- Rezervasyon listesini kontrol edin
- Rezervasyon detaylarını açın

## Yöntem 3: Örnek Veri Kontrolü

### Kritik Tablolar

**1. Kullanıcılar (AspNetUsers)**
```sql
-- SQL Server
SELECT COUNT(*) FROM AspNetUsers;
SELECT Id, UserName, Email FROM AspNetUsers;

-- MySQL
SELECT COUNT(*) FROM AspNetUsers;
SELECT Id, UserName, Email FROM AspNetUsers;
```

**2. İşletmeler (Businesses)**
```sql
-- SQL Server
SELECT COUNT(*) FROM Businesses;
SELECT TOP 10 Id, Name, CategoryId FROM Businesses;

-- MySQL
SELECT COUNT(*) FROM Businesses;
SELECT Id, Name, CategoryId FROM Businesses LIMIT 10;
```

**3. Yorumlar (Reviews)**
```sql
-- SQL Server
SELECT COUNT(*) FROM Reviews;
SELECT TOP 10 Id, BusinessId, UserId, Rating, Comment FROM Reviews;

-- MySQL
SELECT COUNT(*) FROM Reviews;
SELECT Id, BusinessId, UserId, Rating, Comment FROM Reviews LIMIT 10;
```

## Yöntem 4: Detaylı Veri Karşılaştırması

### Örnek: Belirli Bir İşletmeyi Kontrol Etme

**SQL Server:**
```sql
SELECT * FROM Businesses WHERE Id = 1;
SELECT * FROM Reviews WHERE BusinessId = 1;
SELECT * FROM BusinessImages WHERE BusinessId = 1;
```

**MySQL:**
```sql
SELECT * FROM Businesses WHERE Id = 1;
SELECT * FROM Reviews WHERE BusinessId = 1;
SELECT * FROM BusinessImages WHERE BusinessId = 1;
```

Her iki sorgunun sonuçlarını karşılaştırın.

## Yöntem 5: Toplam Kayıt Sayısı Kontrolü

### SQL Server:
```sql
SELECT 
    (SELECT COUNT(*) FROM AspNetUsers) +
    (SELECT COUNT(*) FROM Businesses) +
    (SELECT COUNT(*) FROM Reviews) +
    (SELECT COUNT(*) FROM Reservations) +
    (SELECT COUNT(*) FROM News) AS TotalRecords;
```

### MySQL:
```sql
SELECT 
    (SELECT COUNT(*) FROM AspNetUsers) +
    (SELECT COUNT(*) FROM Businesses) +
    (SELECT COUNT(*) FROM Reviews) +
    (SELECT COUNT(*) FROM Reservations) +
    (SELECT COUNT(*) FROM News) AS TotalRecords;
```

## Sorun Tespiti

### Veri Eksikse:

1. **Migration tamamlandı mı kontrol edin:**
   ```bash
   dotnet ef migrations list
   ```

2. **Veritabanı bağlantısını kontrol edin:**
   - `appsettings.Development.json` dosyasındaki connection string'i kontrol edin
   - MySQL servisinin çalıştığından emin olun

3. **Veri taşıma işlemini tekrar yapın:**
   - MySQL Workbench Migration Wizard kullanın
   - Veya manuel SQL export/import yapın

### Veri Bozuksa:

1. **Foreign key ilişkilerini kontrol edin:**
   ```sql
   -- MySQL'de
   SELECT * FROM Businesses WHERE CategoryId NOT IN (SELECT Id FROM Categories);
   ```

2. **Veri tiplerini kontrol edin:**
   - Özellikle tarih/saat alanları
   - Decimal/numeric alanlar
   - Text alanlar

## Önemli Notlar

⚠️ **Yedek:** Veri kontrolü yapmadan önce MySQL veritabanının yedeğini alın

⚠️ **Test:** Önce test veritabanında kontrol yapın

⚠️ **Sıralama:** SQL Server ve MySQL'deki sorgu sonuçları farklı sırada olabilir, bu normaldir

## Hızlı Kontrol Listesi

- [ ] Kullanıcı sayıları eşleşiyor mu?
- [ ] İşletme sayıları eşleşiyor mu?
- [ ] Yorum sayıları eşleşiyor mu?
- [ ] Rezervasyon sayıları eşleşiyor mu?
- [ ] Haber sayıları eşleşiyor mu?
- [ ] Kategori sayıları eşleşiyor mu?
- [ ] Toplam kayıt sayısı eşleşiyor mu?
- [ ] Login işlemi çalışıyor mu?
- [ ] İşletme detayları açılıyor mu?
- [ ] Yorumlar görüntüleniyor mu?

Tüm kontroller başarılıysa, veri taşıma işlemi tamamlanmış demektir! 🎉
