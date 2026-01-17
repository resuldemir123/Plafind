# Basit Test Istatistikleri
param(
    [switch]$Coverage
)

Write-Host ""
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "TEST ISTATISTIKLERI" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host ""

# Test dosyalarini analiz et
$testFiles = Get-ChildItem -Path . -Filter "*Tests.cs" -Recurse | Where-Object { 
    $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*\obj\*" 
}

$stats = @{
    TotalClasses = 0
    TotalMethods = 0
    Models = 0
    Services = 0
    Controllers = 0
    Data = 0
    Other = 0
}

$classDetails = @()

foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    $factCount = ([regex]::Matches($content, '\[Fact\]')).Count
    $theoryCount = ([regex]::Matches($content, '\[Theory\]')).Count
    $total = $factCount + $theoryCount
    
    $category = "Other"
    if ($file.DirectoryName -like "*\Models\*") { $category = "Models" }
    elseif ($file.DirectoryName -like "*\Services\*") { $category = "Services" }
    elseif ($file.DirectoryName -like "*\Controllers\*") { $category = "Controllers" }
    elseif ($file.DirectoryName -like "*\Data\*") { $category = "Data" }
    
    $stats[$category] += $total
    $stats.TotalClasses++
    $stats.TotalMethods += $total
    
    $classDetails += [PSCustomObject]@{
        Class = $file.BaseName
        Category = $category
        Methods = $total
        File = $file.Name
    }
}

# Test calistir
Write-Host "Testler calistiriliyor..." -ForegroundColor Yellow
$testOutput = dotnet test --logger "console;verbosity=minimal" 2>&1 | Out-String

# Sonuclari parse et
$passed = 0
$failed = 0
$skipped = 0
$duration = 0

if ($testOutput -match "Basarili:\s*(\d+)") { $passed = [int]$matches[1] }
if ($testOutput -match "Basarisiz:\s*(\d+)") { $failed = [int]$matches[1] }
if ($testOutput -match "Atlanan:\s*(\d+)") { $skipped = [int]$matches[1] }
if ($testOutput -match "Sure:\s*(\d+)\s*s") { $duration = [int]$matches[1] }

$total = $passed + $failed + $skipped
$successRate = if ($total -gt 0) { [math]::Round(($passed / $total) * 100, 2) } else { 0 }

Write-Host ""
Write-Host "GENEL ISTATISTIKLER" -ForegroundColor Yellow
Write-Host "-------------------------------------------------------" -ForegroundColor Gray
Write-Host "Test Siniflari:      $($stats.TotalClasses)" -ForegroundColor White
Write-Host "Test Metodlari:      $($stats.TotalMethods)" -ForegroundColor White
Write-Host "Toplam Test:         $total" -ForegroundColor White
Write-Host "Basarili:            $passed" -ForegroundColor Green
Write-Host "Basarisiz:           $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
Write-Host "Atlanan:             $skipped" -ForegroundColor Yellow
Write-Host "Basar Orani:         $successRate%" -ForegroundColor $(if ($successRate -eq 100) { "Green" } else { "Yellow" })
Write-Host "Toplam Sure:         $duration saniye" -ForegroundColor White
if ($total -gt 0) {
    Write-Host "Ortalama Sure:       $([math]::Round($duration / $total, 3)) saniye/test" -ForegroundColor White
}
Write-Host ""

Write-Host "KATEGORI DAGILIMI" -ForegroundColor Yellow
Write-Host "-------------------------------------------------------" -ForegroundColor Gray
foreach ($cat in @("Models", "Services", "Controllers", "Data", "Other")) {
    if ($stats[$cat] -gt 0) {
        $pct = [math]::Round(($stats[$cat] / $stats.TotalMethods) * 100, 1)
        $percentText = "$pct%"
        Write-Host "$($cat.PadRight(15)) $($stats[$cat]) test ($percentText)" -ForegroundColor White
    }
}
Write-Host ""

Write-Host "TEST SINIFLARI" -ForegroundColor Yellow
Write-Host "-------------------------------------------------------" -ForegroundColor Gray
foreach ($detail in $classDetails | Sort-Object Category, Class) {
    Write-Host "[$($detail.Category)] $($detail.Class)" -ForegroundColor Cyan
    Write-Host "  $($detail.Methods) test metodu - $($detail.File)" -ForegroundColor Gray
}
Write-Host ""

# Coverage
if ($Coverage) {
    Write-Host "COVERAGE RAPORU" -ForegroundColor Yellow
    Write-Host "-------------------------------------------------------" -ForegroundColor Gray
    if (Test-Path "./TestResults/coverage.json") {
        $cov = Get-Content "./TestResults/coverage.json" | ConvertFrom-Json
        Write-Host "Line Coverage:    $([math]::Round($cov.summary.linecoverage, 2))%" -ForegroundColor White
        Write-Host "Branch Coverage:  $([math]::Round($cov.summary.branchcoverage, 2))%" -ForegroundColor White
    } else {
        Write-Host "Coverage raporu bulunamadi." -ForegroundColor Yellow
        Write-Host "Calistirin: dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=json /p:CoverletOutput=./TestResults/coverage" -ForegroundColor Gray
    }
    Write-Host ""
}

Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host ""
