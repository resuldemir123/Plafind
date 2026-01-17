# 🧪 Plafind Test Rehberi

Bu rehber, Plafind sistemini test etmek için oluşturulmuştur.

## Test Yapısı

Test projesi `Plafind.Tests` klasöründe bulunmaktadır ve şu testleri içerir:

### 1. Unit Testler
- **Services/BusinessServiceTests.cs**: İşletme servisi testleri
- **Models/BusinessTests.cs**: İşletme modeli validasyon testleri
- **Data/DbSeederTests.cs**: Veritabanı seeder testleri

### 2. Integration Testler
- **Controllers/HomeControllerTests.cs**: Home controller testleri

### 3. Test Helper'lar
- **Helpers/TestDbContextFactory.cs**: InMemory veritabanı oluşturma
- **Helpers/TestDataBuilder.cs**: Test verileri oluşturma

## Test Çalıştırma

### Yöntem 1: Visual Studio
1. Visual Studio'da solution'ı açın
2. Test Explorer'ı açın (Test > Test Explorer)
3. Tüm testleri çalıştırın veya belirli testleri seçin

### Yöntem 2: Command Line

```bash
# Test projesine git
cd Plafind.Tests

# Tüm testleri çalıştır
dotnet test

# Detaylı çıktı ile
dotnet test --logger "console;verbosity=detailed"

# Belirli bir test sınıfını çalıştır
dotnet test --filter FullyQualifiedName~BusinessServiceTests

# Coverage raporu ile
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Yöntem 3: PowerShell Script

```powershell
.\Plafind.Tests\run-tests.ps1
```

## Test Senaryoları

### 1. İşletme Servisi Testleri
- ✅ İşletme ID ile getirme
- ✅ Olmayan işletme için null dönme
- ✅ Görüntülenme sayısını artırma

### 2. Veritabanı Seeder Testleri
- ✅ Kategoriler oluşturma
- ✅ İşletmeler oluşturma
- ✅ Tekrar çalıştırmada duplicate önleme

### 3. Model Validasyon Testleri
- ✅ İşletme modeli gerekli alanlar
- ✅ Varsayılan değerler
- ✅ Puan aralığı kontrolü

## Test Verileri

Testler **InMemory** veritabanı kullanır, gerçek veritabanınıza dokunmaz.

## Sorun Giderme

### Döngüsel Bağımlılık Hatası
Eğer döngüsel bağımlılık hatası alırsanız:

1. Solution'ı temizleyin:
```bash
dotnet clean
```

2. NuGet paketlerini yeniden yükleyin:
```bash
dotnet restore
```

3. Test projesini ayrı olarak build edin:
```bash
cd Plafind.Tests
dotnet build --no-dependencies
```

### Test Çalışmıyor
1. Ana projeyi önce build edin:
```bash
cd Plafind
dotnet build
```

2. Sonra test projesini build edin:
```bash
cd ../Plafind.Tests
dotnet build
```

## Yeni Test Ekleme

### Unit Test Örneği

```csharp
using Xunit;
using Plafind.Tests.Helpers;

namespace Plafind.Tests.Services
{
    public class MyServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public MyServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
        }

        [Fact]
        public async Task MyTest_ShouldWork()
        {
            // Arrange
            // Test verilerini hazırla

            // Act
            // Test edilecek metodu çağır

            // Assert
            // Sonuçları kontrol et
        }

        public void Dispose()
        {
            _context?.Database.EnsureDeleted();
            _context?.Dispose();
        }
    }
}
```

## Test Coverage

Test coverage raporu oluşturmak için:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

Sonuçlar `coverage.opencover.xml` dosyasında olacaktır.

## Notlar

- Testler gerçek veritabanını kullanmaz
- Her test izole çalışır
- Test verileri her test sonunda temizlenir
- InMemory veritabanı kullanılır

