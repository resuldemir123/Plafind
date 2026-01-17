# Test çalıştırma scripti
Write-Host "🧪 Plafind Testleri Çalıştırılıyor..." -ForegroundColor Cyan

# Tüm testleri çalıştır
dotnet test --logger "console;verbosity=normal"

# Test sonuçlarını göster
if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Tüm testler başarıyla geçti!" -ForegroundColor Green
} else {
    Write-Host "`n❌ Bazı testler başarısız oldu!" -ForegroundColor Red
    exit $LASTEXITCODE
}

