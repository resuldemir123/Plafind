# Migration dosyalarını temizleme scripti
Write-Host "Migration dosyaları temizleniyor..." -ForegroundColor Yellow

$migrationsPath = "Migrations"
if (Test-Path $migrationsPath) {
    Get-ChildItem -Path $migrationsPath -File -Recurse | Remove-Item -Force
    Write-Host "Tüm migration dosyaları silindi!" -ForegroundColor Green
} else {
    Write-Host "Migrations klasörü bulunamadı!" -ForegroundColor Red
}

Write-Host ""
Write-Host "Şimdi yeni MySQL migration'ı oluşturun:" -ForegroundColor Cyan
Write-Host "dotnet ef migrations add InitialMySQLMigration" -ForegroundColor White
