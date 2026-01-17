# Detaylı Test İstatistikleri Analizi
Write-Host "📊 Test İstatistikleri Analizi" -ForegroundColor Cyan
Write-Host "=" * 50 -ForegroundColor Cyan
Write-Host ""

# Test dosyalarını bul
$testFiles = Get-ChildItem -Path . -Filter "*Tests.cs" -Recurse | Where-Object { $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*\obj\*" }

$totalTestClasses = 0
$totalTestMethods = 0
$testCategories = @{
    "Models" = 0
    "Services" = 0
    "Controllers" = 0
    "Data" = 0
    "Other" = 0
}

$testDetails = @()

foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    $className = $file.BaseName
    
    # Test metodlarını say
    $factCount = ([regex]::Matches($content, '\[Fact\]')).Count
    $theoryCount = ([regex]::Matches($content, '\[Theory\]')).Count
    $totalMethods = $factCount + $theoryCount
    
    # Kategori belirle
    $category = "Other"
    if ($file.DirectoryName -like "*\Models\*") { $category = "Models" }
    elseif ($file.DirectoryName -like "*\Services\*") { $category = "Services" }
    elseif ($file.DirectoryName -like "*\Controllers\*") { $category = "Controllers" }
    elseif ($file.DirectoryName -like "*\Data\*") { $category = "Data" }
    
    $testCategories[$category] += $totalMethods
    $totalTestClasses++
    $totalTestMethods += $totalMethods
    
    # Test metod isimlerini çıkar
    $methodNames = @()
    $methodMatches = [regex]::Matches($content, '\[Fact\][\s\S]*?public\s+(?:async\s+)?(?:Task\s+)?\w+\s+(\w+)\([^)]*\)')
    foreach ($match in $methodMatches) {
        if ($match.Groups.Count -gt 1) {
            $methodNames += $match.Groups[1].Value
        }
    }
    
    $testDetails += [PSCustomObject]@{
        ClassName = $className
        FileName = $file.Name
        Category = $category
        FactCount = $factCount
        TheoryCount = $theoryCount
        TotalMethods = $totalMethods
        Methods = $methodNames -join ", "
    }
}

# Test çalıştır ve sonuçları al
Write-Host "🧪 Testler çalıştırılıyor..." -ForegroundColor Yellow
$testResult = dotnet test --logger "console;verbosity=minimal" 2>&1

# Sonuçları parse et
$passed = 0
$failed = 0
$skipped = 0
$duration = 0

$testResultString = $testResult -join "`n"

if ($testResultString -match "Geçti:\s*(\d+)") {
    $passed = [int]$matches[1]
}
if ($testResultString -match "Başarısız:\s*(\d+)") {
    $failed = [int]$matches[1]
}
if ($testResultString -match "Atlandı:\s*(\d+)") {
    $skipped = [int]$matches[1]
}
# Süre için farklı formatları dene
$durationPattern = '(\d+[,\\.]\d+)\s*Saniye'
$durationMatch = [regex]::Match($testResultString, $durationPattern)
if ($durationMatch.Success) {
    $duration = [double]$durationMatch.Groups[1].Value.Replace(",", ".")
}

$total = $passed + $failed + $skipped
$successRate = if ($total -gt 0) { [math]::Round(($passed / $total) * 100, 2) } else { 0 }

# İstatistikleri göster
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📈 GENEL İSTATİSTİKLER" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test Sınıfları:     $totalTestClasses" -ForegroundColor White
Write-Host "Test Metodları:      $totalTestMethods" -ForegroundColor White
Write-Host "Toplam Test:         $total" -ForegroundColor White
Write-Host "Başarılı:            $passed" -ForegroundColor Green
Write-Host "Başarısız:           $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
Write-Host "Atlanan:             $skipped" -ForegroundColor Yellow
Write-Host "Başarı Oranı:        $successRate%" -ForegroundColor $(if ($successRate -eq 100) { "Green" } else { "Yellow" })
Write-Host "Toplam Süre:         $([math]::Round($duration, 2)) saniye" -ForegroundColor White
Write-Host "Ortalama Süre:       $([math]::Round($duration / $total, 3)) saniye/test" -ForegroundColor White
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📁 KATEGORİ BAZLI DAĞILIM" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

foreach ($category in $testCategories.Keys | Sort-Object) {
    $count = $testCategories[$category]
    if ($count -gt 0) {
        $percentage = [math]::Round(($count / $totalTestMethods) * 100, 1)
        $bar = "█" * [math]::Floor($percentage / 2)
        Write-Host "$category".PadRight(15) -NoNewline -ForegroundColor White
        Write-Host "$bar " -NoNewline -ForegroundColor Cyan
        $percentText = "$percentage%"
        Write-Host "$count test ($percentText)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📋 TEST SINIFLARI DETAYI" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

foreach ($detail in $testDetails | Sort-Object Category, ClassName) {
    Write-Host "[$($detail.Category)] " -NoNewline -ForegroundColor Cyan
    Write-Host "$($detail.ClassName)" -ForegroundColor White
    Write-Host "   📄 $($detail.FileName)" -ForegroundColor Gray
    Write-Host "   🧪 $($detail.TotalMethods) test metodu " -NoNewline -ForegroundColor Yellow
    if ($detail.FactCount -gt 0) {
        Write-Host "($($detail.FactCount) Fact" -NoNewline -ForegroundColor Gray
        if ($detail.TheoryCount -gt 0) {
            Write-Host ", $($detail.TheoryCount) Theory" -NoNewline -ForegroundColor Gray
        }
        Write-Host ")" -ForegroundColor Gray
    }
    Write-Host ""
}

# JSON raporu oluştur
$jsonReport = @{
    GeneratedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Summary = @{
        TotalTestClasses = $totalTestClasses
        TotalTestMethods = $totalTestMethods
        TotalTests = $total
        Passed = $passed
        Failed = $failed
        Skipped = $skipped
        SuccessRate = $successRate
        Duration = $duration
        AverageDuration = [math]::Round($duration / $total, 3)
    }
    Categories = $testCategories
    TestClasses = $testDetails | ForEach-Object {
        @{
            ClassName = $_.ClassName
            FileName = $_.FileName
            Category = $_.Category
            FactCount = $_.FactCount
            TheoryCount = $_.TheoryCount
            TotalMethods = $_.TotalMethods
            Methods = $_.Methods
        }
    }
}

$jsonPath = "./TestResults/test-statistics.json"
$jsonReport | ConvertTo-Json -Depth 10 | Out-File -FilePath $jsonPath -Encoding UTF8

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ JSON raporu kaydedildi: $jsonPath" -ForegroundColor Green
Write-Host ""

