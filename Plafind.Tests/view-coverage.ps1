# Coverage Raporu Görüntüleme
Write-Host "📊 Code Coverage Raporu" -ForegroundColor Cyan
Write-Host "=" * 50 -ForegroundColor Cyan
Write-Host ""

if (Test-Path "./TestResults/coverage.json") {
    $coverage = Get-Content "./TestResults/coverage.json" | ConvertFrom-Json
    
    Write-Host "Genel Coverage:" -ForegroundColor Yellow
    Write-Host "  Line Coverage:    $([math]::Round($coverage.summary.linecoverage, 2))%" -ForegroundColor White
    Write-Host "  Branch Coverage:  $([math]::Round($coverage.summary.branchcoverage, 2))%" -ForegroundColor White
    Write-Host "  Method Coverage:  $([math]::Round($coverage.summary.methodcoverage, 2))%" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Dosya Bazlı Coverage:" -ForegroundColor Yellow
    foreach ($module in $coverage.modules) {
        Write-Host "  $($module.name)" -ForegroundColor Cyan
        Write-Host "    Line: $([math]::Round($module.summary.linecoverage, 2))% | Branch: $([math]::Round($module.summary.branchcoverage, 2))%" -ForegroundColor Gray
    }
} else {
    Write-Host "Coverage raporu bulunamadı. Önce testleri coverage ile çalıştırın:" -ForegroundColor Yellow
    Write-Host "  dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=json /p:CoverletOutput=./TestResults/coverage" -ForegroundColor White
}

