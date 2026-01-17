# Plafind Test Projesi

Bu proje Plafind uygulaması için testleri içerir.

## Test Çalıştırma

### Tüm testleri çalıştır:
```bash
dotnet test
```

### Belirli bir test sınıfını çalıştır:
```bash
dotnet test --filter FullyQualifiedName~BusinessServiceTests
```

### Detaylı çıktı ile:
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Coverage raporu ile:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Test Yapısı

- **Services/**: Servis katmanı testleri
- **Controllers/**: Controller testleri
- **Models/**: Model validasyon testleri
- **Data/**: Veritabanı seeder testleri
- **Helpers/**: Test helper sınıfları

## Test Verileri

Testler InMemory veritabanı kullanır, gerçek veritabanına dokunmaz.

