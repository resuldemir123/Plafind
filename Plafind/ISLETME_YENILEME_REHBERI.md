# İşletme Verilerini Yenileme Rehberi

Mevcut tüm işletmeleri silip yeni gerçek veriler eklemek için bu rehberi kullanın.

## Yöntem 1: Admin Panel Üzerinden (ÖNERİLEN)

### Adımlar:

1. **Uygulamayı çalıştırın:**
   ```bash
   dotnet run
   ```

2. **Admin olarak giriş yapın**

3. **Admin Panel > Businesses** sayfasına gidin

4. **"Gerçek İşletmeleri Ekle"** butonuna tıklayın
   - Bu buton henüz eklenmediyse, aşağıdaki URL'yi direkt kullanın:
   ```
   POST /Admin/SeedRealBusinesses
   ```

## Yöntem 2: Program.cs ile Otomatik (Development)

### Adımlar:

1. **appsettings.Development.json** dosyasını açın

2. **SeedRealBusinesses** ayarını ekleyin:
   ```json
   {
     "SeedRealBusinesses": true
   }
   ```

3. **Uygulamayı çalıştırın:**
   ```bash
   dotnet run
   ```

   Uygulama başladığında otomatik olarak:
   - Mevcut tüm işletmeler silinecek
   - Yeni gerçek işletmeler eklenecek

## Yöntem 3: SQL Script ile (Manuel)

### Adım 1: Mevcut İşletmeleri Sil

**MySQL Workbench'te:**

```sql
USE AlanyaBusinessGuide;

-- Foreign key kontrollerini kapat
SET FOREIGN_KEY_CHECKS = 0;

-- İlgili tabloları temizle
DELETE FROM Reviews;
DELETE FROM Reservations;
DELETE FROM UserFavorites;
DELETE FROM BusinessImages;
DELETE FROM Businesses;

-- Foreign key kontrollerini aç
SET FOREIGN_KEY_CHECKS = 1;
```

### Adım 2: Kategorileri Kontrol Et

```sql
-- Gerekli kategorileri kontrol et ve ekle
INSERT IGNORE INTO Categories (Name, Description) VALUES
('Restoran', 'Yemek ve içecek hizmetleri'),
('Otel', 'Konaklama hizmetleri'),
('Alışveriş Merkezi', 'Alışveriş merkezleri ve AVM''ler'),
('Outlet', 'Outlet mağazaları ve indirimli alışveriş'),
('Marina', 'Yat limanı ve marina hizmetleri'),
('Mağaza', 'Giyim ve genel mağazalar');
```

### Adım 3: Yeni İşletmeleri Ekle

`Scripts/SeedRealBusinesses.cs` dosyasındaki INSERT komutlarını MySQL formatına çevirip çalıştırın.

## Yöntem 4: C# Console Uygulaması (Gelişmiş)

Eğer bağımsız bir script çalıştırmak isterseniz, yeni bir console projesi oluşturup `SeedRealBusinesses.cs` dosyasını kullanabilirsiniz.

## Eklenen İşletmeler

Toplam **20 gerçek işletme** eklenecek:

### Restoranlar (9 adet):
1. Le Chevy Restaurant
2. Ravza Restaurant
3. Mezze Grill Restaurant & Ocakbaşı
4. Hasır Restaurant & Bar
5. Alanya Olivia Gourmet Restaurant & Cafe Bar
6. Soul of Kitchen Restaurant
7. Kale Panorama Restaurant
8. Kleopatra Blue Hawaii Restaurant
9. Crazy Horse / Crazy Sushi

### Oteller (3 adet):
1. Sunprime C-Lounge Hotel
2. Sirius Deluxe Hotel
3. Cleopatra Blue Hawaii Hotel

### Alışveriş Merkezleri (3 adet):
1. Alanyum Alışveriş ve Eğlence Merkezi
2. Mall of Alanya
3. Yekta Mall Alışveriş ve Eğlence Merkezi

### Outlet Mağazaları (2 adet):
1. Neva Outlet Alanya (Keykubat)
2. Neva Outlet Okurcalar Soho

### Diğer (3 adet):
1. Alanya Marina
2. LC Waikiki Alanya Atatürk
3. Boyner Alanya (Alanyum AVM Şubesi)

## Kategori Eşleştirmeleri

- `restaurant` → **Restoran**
- `hotel` → **Otel**
- `shopping_mall` → **Alışveriş Merkezi**
- `outlet_store` → **Outlet**
- `marina` → **Marina**
- `restaurant_bar` → **Restoran**
- `clothing_store` → **Mağaza**
- `department_store` → **Mağaza**

## Önemli Notlar

⚠️ **Yedek Alın:** İşletmeleri silmeden önce mutlaka veritabanının yedeğini alın!

⚠️ **İlişkili Veriler:** İşletmeler silindiğinde, yorumlar, rezervasyonlar ve favoriler de silinecek.

⚠️ **Onay Durumu:** Yeni eklenen işletmeler otomatik olarak `IsApproved = true` olarak işaretlenir.

## Sorun Giderme

### "Foreign key constraint fails"

**Çözüm:** İlişkili tabloları önce temizleyin:
```sql
DELETE FROM Reviews;
DELETE FROM Reservations;
DELETE FROM UserFavorites;
DELETE FROM BusinessImages;
DELETE FROM Businesses;
```

### "Category not found"

**Çözüm:** Kategorileri önce oluşturun (yukarıdaki SQL script'ini çalıştırın).

### İşletmeler Görünmüyor

**Çözüm:** 
- `IsActive = true` kontrolü yapın
- `IsApproved = true` kontrolü yapın
- Sayfayı yenileyin

## Sonraki Adımlar

1. ✅ İşletmeleri ekledikten sonra uygulamayı test edin
2. ✅ İşletme detay sayfalarını kontrol edin
3. ✅ Arama fonksiyonunu test edin
4. ✅ Kategori filtrelerini kontrol edin

Başarılar! 🎉
