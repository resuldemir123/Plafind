# MySQL Migration Oluşturma Scripti
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MySQL Migration Oluşturuluyor..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Build kontrolü
Write-Host "1. Proje build ediliyor..." -ForegroundColor Yellow
dotnet build --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build başarısız!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build başarılı!" -ForegroundColor Green
Write-Host ""

# Migration oluştur
Write-Host "2. MySQL migration oluşturuluyor..." -ForegroundColor Yellow
$result = dotnet ef migrations add InitialMySQLMigration 2>&1
$result | Write-Host

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✓ Migration başarıyla oluşturuldu!" -ForegroundColor Green
    Write-Host ""
    Write-Host "3. Şimdi veritabanını oluşturun:" -ForegroundColor Cyan
    Write-Host "   dotnet ef database update" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "✗ Migration oluşturulamadı!" -ForegroundColor Red
    Write-Host "Hata detayları yukarıda görüntüleniyor." -ForegroundColor Yellow
}
