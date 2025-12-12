# LocalDB Instance'larını Bulma Scripti
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LocalDB Instance'ları Aranıyor..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# sqllocaldb komutunu kontrol et
if (Get-Command sqllocaldb -ErrorAction SilentlyContinue) {
    Write-Host "Tüm LocalDB Instance'ları:" -ForegroundColor Yellow
    sqllocaldb info
    Write-Host ""
    
    Write-Host "MSSQLLocalDB Instance Durumu:" -ForegroundColor Yellow
    sqllocaldb info MSSQLLocalDB
    Write-Host ""
    
    Write-Host "Instance'ı başlatmak için:" -ForegroundColor Cyan
    Write-Host "sqllocaldb start MSSQLLocalDB" -ForegroundColor White
} else {
    Write-Host "sqllocaldb komutu bulunamadı!" -ForegroundColor Red
    Write-Host "SQL Server LocalDB kurulu olmayabilir." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Alternatif: Visual Studio SQL Server Object Explorer kullanın:" -ForegroundColor Cyan
    Write-Host "1. Visual Studio'yu açın" -ForegroundColor White
    Write-Host "2. View > SQL Server Object Explorer" -ForegroundColor White
    Write-Host "3. LocalDB instance'ını bulun" -ForegroundColor White
}

Write-Host ""
Write-Host "Connection String Örnekleri:" -ForegroundColor Yellow
Write-Host "(localdb)\MSSQLLocalDB" -ForegroundColor White
Write-Host "(localdb)\ProjectsV13" -ForegroundColor White
Write-Host "(localdb)\v11.0" -ForegroundColor White
