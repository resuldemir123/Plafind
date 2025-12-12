# LocalDB'ye Bağlanma Rehberi

Visual Studio'nun LocalDB'sindeki verilere erişmek için birkaç yöntem var.

## Yöntem 1: Visual Studio SQL Server Object Explorer (EN KOLAY)

### Adımlar:

1. **Visual Studio'yu açın**

2. **View > SQL Server Object Explorer** menüsüne gidin
   - Veya `Ctrl+\, Ctrl+S` kısayolunu kullanın

3. **SQL Server** sekmesinde **LocalDB** instance'ını bulun:
   - Genellikle şu formatta görünür: `(localdb)\MSSQLLocalDB` veya `(localdb)\ProjectsV13`

4. **Databases** altında `AlanyaBusinessGuide` veritabanını bulun

5. **Tables** altında tabloları görebilirsiniz

6. **Sağ tıklayıp "View Data"** ile verileri görüntüleyebilirsiniz

## Yöntem 2: SQL Server Management Studio ile Bağlanma

### Connection String:

```
Server=(localdb)\MSSQLLocalDB;Database=AlanyaBusinessGuide;Trusted_Connection=True;
```

### Adımlar:

1. **SQL Server Management Studio'yu açın**

2. **Connect to Server** penceresinde:
   - **Server type:** Database Engine
   - **Server name:** `(localdb)\MSSQLLocalDB`
   - **Authentication:** Windows Authentication

3. **Connect** butonuna tıklayın

4. **Databases > AlanyaBusinessGuide** altında tabloları görebilirsiniz

### Eğer Bağlanamazsanız:

LocalDB instance adını bulmak için PowerShell'de şu komutu çalıştırın:

```powershell
sqllocaldb info
```

veya

```powershell
sqllocaldb info MSSQLLocalDB
```

Çıkan instance adını kullanın (örneğin: `(localdb)\MSSQLLocalDB` veya `(localdb)\ProjectsV13`)

## Yöntem 3: LocalDB Instance Adını Bulma

### PowerShell Komutu:

```powershell
sqllocaldb info
```

Bu komut tüm LocalDB instance'larını listeler.

### LocalDB Instance'ını Başlatma:

Eğer instance çalışmıyorsa:

```powershell
sqllocaldb start MSSQLLocalDB
```

## Yöntem 4: Connection String ile Doğrudan Bağlanma

### Visual Studio'da:

1. **Server Explorer** penceresini açın
2. **Data Connections** üzerine sağ tıklayın
3. **Add Connection** seçin
4. **Microsoft SQL Server** seçin
5. **Server name:** `(localdb)\MSSQLLocalDB`
6. **Database name:** `AlanyaBusinessGuide`
7. **Test Connection** ile bağlantıyı test edin

## Verileri Export Etme (Visual Studio'dan)

### SQL Server Object Explorer'dan:

1. Tabloya sağ tıklayın
2. **View Data** seçin
3. Sonuçları görüntüleyin
4. **Export to CSV** veya **Copy** ile verileri kopyalayın

### SQL Query ile:

1. Tabloya sağ tıklayın
2. **New Query** seçin
3. Şu sorguyu çalıştırın:

```sql
SELECT * FROM AspNetUsers;
```

4. Sonuçları **Copy** veya **Save Results As** ile kaydedin

## Hızlı Kontrol: Veri Sayılarını Görme

Visual Studio SQL Server Object Explorer'da:

1. Tabloya sağ tıklayın
2. **View Data** seçin
3. Alt kısımda kayıt sayısını görebilirsiniz

Veya SQL Query ile:

```sql
SELECT COUNT(*) FROM AspNetUsers;
SELECT COUNT(*) FROM Businesses;
SELECT COUNT(*) FROM Reviews;
```

## Sorun Giderme

### "Cannot connect to (localdb)\MSSQLLocalDB"

**Çözüm 1:** Instance'ı başlatın:
```powershell
sqllocaldb start MSSQLLocalDB
```

**Çözüm 2:** Farklı instance adını deneyin:
- `(localdb)\ProjectsV13`
- `(localdb)\v11.0`
- `(localdb)\MSSQLLocalDB`

**Çözüm 3:** Visual Studio SQL Server Object Explorer kullanın

### "Database AlanyaBusinessGuide does not exist"

**Çözüm:** Veritabanı farklı bir instance'da olabilir. Tüm instance'ları kontrol edin:

```powershell
sqllocaldb info
```

Her instance için bağlanıp veritabanını arayın.

### Veriler Görünmüyor

**Çözüm:** 
1. Doğru veritabanına bağlandığınızdan emin olun
2. Tabloların içinde veri olup olmadığını kontrol edin:
   ```sql
   SELECT COUNT(*) FROM TableName;
   ```

## Önerilen Yöntem

**En kolay:** Visual Studio SQL Server Object Explorer kullanın. Bu, LocalDB'ye doğrudan erişim sağlar ve verileri kolayca görüntülemenize olanak tanır.
