# MySQL Migration Sorunu Çözümü

## Sorun
Mevcut migration dosyaları SQL Server için oluşturulmuş ve `nvarchar(max)`, `datetime2` gibi SQL Server'a özgü veri tiplerini kullanıyor. MySQL'de bunlar geçerli değil.

## Çözüm: Migration'ları Temizle ve Yeniden Oluştur

### Adım 1: Migration Klasörünü Temizle

**Manuel olarak:**
1. `Plafind/Migrations` klasöründeki **TÜM** `.cs` dosyalarını silin
2. `ApplicationDbContextModelSnapshot.cs` dosyasını da silin

**PowerShell ile:**
```powershell
cd Plafind
Remove-Item Migrations\*.cs -Force
```

### Adım 2: Yeni MySQL Migration Oluştur

```bash
dotnet ef migrations add InitialMySQLMigration
```

Bu komut MySQL için uygun migration dosyaları oluşturacak.

### Adım 3: Veritabanını Oluştur

```bash
dotnet ef database update
```

## Alternatif Çözüm: Migration Dosyalarını Manuel Düzenle

Eğer migration dosyalarını silmek istemiyorsanız, her migration dosyasındaki SQL Server syntax'ını MySQL syntax'ına çevirmeniz gerekir:

### Veri Tipi Dönüşümleri

| SQL Server | MySQL |
|------------|-------|
| `nvarchar(max)` | `longtext` |
| `nvarchar(n)` | `varchar(n)` |
| `datetime2` | `datetime` |
| `SqlServer:Identity` | `MySql:ValueGenerationStrategy` |

### Örnek Dönüşüm

**SQL Server:**
```csharp
AdminUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
```

**MySQL:**
```csharp
AdminUserId = table.Column<string>(type: "longtext", nullable: true)
```

## Önerilen Yöntem

**En kolay ve güvenli yöntem:** Migration klasörünü temizleyip yeni migration oluşturmak. Bu şekilde:
- ✅ Tüm tablolar MySQL için optimize edilmiş olur
- ✅ Veri tipleri doğru olur
- ✅ Identity/auto-increment doğru çalışır
- ✅ Foreign key'ler doğru oluşturulur

## Dikkat!

⚠️ **Veri Kaybı:** Eğer SQL Server veritabanında veri varsa, önce verileri yedekleyin!

⚠️ **Yedek:** Migration dosyalarını silmeden önce bir yedek alın.
