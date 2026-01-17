# Detayli Test Raporu Gosterimi
param(
    [switch]$Coverage,
    [switch]$OpenHtml
)

$ErrorActionPreference = "SilentlyContinue"

Write-Host ""
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "  TEST ISTATISTIK RAPORU" -ForegroundColor Cyan
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
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($content) {
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
}

# Test calistir ve sonuclari al
Write-Host "Testler calistiriliyor..." -ForegroundColor Yellow
$testOutput = dotnet test --verbosity normal 2>&1
$testResult = $testOutput | Out-String

# Sonuclari parse et (Turkce karakterler icin - encoding sorunlarini asmak icin)
$passed = 0
$failed = 0
$skipped = 0
$duration = 0
$total = 0

# Satirlari tek tek kontrol et (encoding sorunlarini asmak icin)
foreach ($line in $testOutput) {
    $lineStr = $line.ToString()
    # Toplam test sayisi
    if ($lineStr -match "Toplam test say.*s.*:\s*(\d+)") { 
        $total = [int]$matches[1]
    }
    # Geçti
    if ($lineStr -match "Ge.*ti:\s*(\d+)") { 
        $passed = [int]$matches[1]
    }
    # Başarısız
    if ($lineStr -match "Ba.*ar.*s.*z:\s*(\d+)") { 
        $failed = [int]$matches[1]
    }
    # Atlanan
    if ($lineStr -match "Atlanan:\s*(\d+)") { 
        $skipped = [int]$matches[1]
    }
    # Toplam süre
    if ($lineStr -match "Toplam s.*re:\s*([\d,]+)\s*Saniye") {
        $durationStr = $matches[1] -replace ',', '.'
        $duration = [double]$durationStr
    }
}

# Eğer total varsa ama passed yoksa ve "Başarılı" mesajı varsa, tüm testler geçmiştir
if ($total -gt 0 -and $passed -eq 0) {
    $successMsg = $testOutput | Where-Object { $_ -match "Ba.*ar.*l.*" -or $_ -match "Successful" }
    if ($successMsg) {
        $passed = $total
    }
}

if ($testResult -match "Basarisiz:\s*(\d+)") { $failed = [int]$matches[1] }
elseif ($testResult -match "Başarısız:\s*(\d+)") { $failed = [int]$matches[1] }
elseif ($testResult -match "Failed:\s*(\d+)") { $failed = [int]$matches[1] }

if ($testResult -match "Atlanan:\s*(\d+)") { $skipped = [int]$matches[1] }
elseif ($testResult -match "Skipped:\s*(\d+)") { $skipped = [int]$matches[1] }

# Sure parse et (virgullu sayilar icin)
if ($testResult -match "Toplam s.*re:\s*([\d,]+)\s*Saniye") { 
    $durationStr = $matches[1] -replace ',', '.'
    $duration = [double]$durationStr
}
elseif ($testResult -match "Toplam süre:\s*([\d,]+)\s*Saniye") {
    $durationStr = $matches[1] -replace ',', '.'
    $duration = [double]$durationStr
}
elseif ($testResult -match "Total time:\s*([\d.]+)\s*seconds") {
    $duration = [double]$matches[1]
}

# Eger total parse edilmediyse, hesapla
if (-not $total -or $total -eq 0) {
    $total = $passed + $failed + $skipped
}
$successRate = if ($total -gt 0) { [math]::Round(($passed / $total) * 100, 2) } else { 0 }

# Raporu goster
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
if ($total -gt 0 -and $duration -gt 0) {
    Write-Host "Ortalama Sure:       $([math]::Round($duration / $total, 3)) saniye/test" -ForegroundColor White
}
Write-Host ""

Write-Host "KATEGORI DAGILIMI" -ForegroundColor Yellow
Write-Host "-------------------------------------------------------" -ForegroundColor Gray
foreach ($cat in @("Models", "Services", "Controllers", "Data", "Other")) {
    if ($stats[$cat] -gt 0) {
        $pct = if ($stats.TotalMethods -gt 0) { [math]::Round(($stats[$cat] / $stats.TotalMethods) * 100, 1) } else { 0 }
        $bar = "█" * [math]::Floor($pct / 5)
        Write-Host "$($cat.PadRight(15)) $($stats[$cat]) test " -NoNewline -ForegroundColor White
        Write-Host "$bar " -NoNewline -ForegroundColor Cyan
        Write-Host "$pct%" -ForegroundColor Gray
    }
}
Write-Host ""

Write-Host "TEST SINIFLARI DETAYI" -ForegroundColor Yellow
Write-Host "-------------------------------------------------------" -ForegroundColor Gray
foreach ($detail in $classDetails | Sort-Object Category, Class) {
    Write-Host "[$($detail.Category)] " -NoNewline -ForegroundColor Cyan
    Write-Host "$($detail.Class)" -ForegroundColor White
    Write-Host "  $($detail.Methods) test metodu - $($detail.File)" -ForegroundColor Gray
}
Write-Host ""

# Coverage
if ($Coverage) {
    Write-Host "CODE COVERAGE" -ForegroundColor Yellow
    Write-Host "-------------------------------------------------------" -ForegroundColor Gray
    
    # Coverage raporu olustur
    Write-Host "Coverage raporu olusturuluyor..." -ForegroundColor Yellow
    
    # TestResults klasorunu olustur
    if (-not (Test-Path "./TestResults")) {
        New-Item -ItemType Directory -Path "./TestResults" -Force | Out-Null
    }
    
    # Coverage raporu olustur (coverlet.collector kullanarak)
    $coverageResult = dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=json /p:CoverletOutput=./TestResults/coverage.json --results-directory ./TestResults --verbosity minimal 2>&1 | Out-String
    
    # Coverage verilerini parse et (console output'tan)
    $lineCov = 0
    $branchCov = 0
    $methodCov = 0
    
    if ($coverageResult -match "Average\s*\|\s*([\d.]+)%\s*\|\s*([\d.]+)%\s*\|\s*([\d.]+)%") {
        $lineCov = [double]$matches[1]
        $branchCov = [double]$matches[2]
        $methodCov = [double]$matches[3]
    }
    
    Start-Sleep -Seconds 2
    
    # Alternatif: coverlet.msbuild kullan
    if (-not (Test-Path "./TestResults/coverage.json")) {
        Write-Host "Alternatif yontem deneniyor..." -ForegroundColor Yellow
        dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=json /p:CoverletOutput=./TestResults/coverage --verbosity minimal 2>&1 | Out-Null
        Start-Sleep -Seconds 2
        
        # Dosya adini kontrol et
        $coverageFiles = Get-ChildItem -Path "./TestResults" -Filter "coverage*.json" -ErrorAction SilentlyContinue
        if ($coverageFiles) {
            Copy-Item $coverageFiles[0].FullName -Destination "./TestResults/coverage.json" -Force
        }
    }
    
    # Eger coverage verileri parse edilmediyse, dosyadan oku
    if ($lineCov -eq 0 -and $branchCov -eq 0 -and (Test-Path "./TestResults/coverage.json")) {
        try {
            $cov = Get-Content "./TestResults/coverage.json" -Raw | ConvertFrom-Json
            
            if ($cov.summary) {
                $lineCov = [math]::Round($cov.summary.linecoverage * 100, 2)
                $branchCov = [math]::Round($cov.summary.branchcoverage * 100, 2)
                $methodCov = [math]::Round($cov.summary.methodcoverage * 100, 2)
            }
        } catch {
            # Hata durumunda console output'tan parse edilen degerleri kullan
        }
    }
    
    if ($lineCov -gt 0 -or $branchCov -gt 0) {
        Write-Host "Line Coverage:      $lineCov%" -ForegroundColor White
        Write-Host "Branch Coverage:    $branchCov%" -ForegroundColor White
        Write-Host "Method Coverage:    $methodCov%" -ForegroundColor White
        Write-Host ""
        
        # Coverage bar goster
        Write-Host "Line Coverage:" -ForegroundColor Gray
        $lineBar = "█" * [math]::Floor($lineCov / 2)
        Write-Host "  $lineBar $lineCov%" -ForegroundColor $(if ($lineCov -ge 80) { "Green" } elseif ($lineCov -ge 50) { "Yellow" } else { "Red" })
        
        Write-Host "Branch Coverage:" -ForegroundColor Gray
        $branchBar = "█" * [math]::Floor($branchCov / 2)
        Write-Host "  $branchBar $branchCov%" -ForegroundColor $(if ($branchCov -ge 80) { "Green" } elseif ($branchCov -ge 50) { "Yellow" } else { "Red" })
    } else {
        Write-Host "Coverage raporu olusturulamadi." -ForegroundColor Yellow
    }
    Write-Host ""
}

# HTML raporu olustur
$htmlPath = "./TestResults/test-report.html"
$htmlContent = @"
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Plafind Test Raporu</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 20px;
            min-height: 100vh;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            border-radius: 15px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            overflow: hidden;
        }
        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }
        .header h1 { font-size: 2.5em; margin-bottom: 10px; }
        .content { padding: 30px; }
        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }
        .stat-card {
            background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
            padding: 25px;
            border-radius: 10px;
            text-align: center;
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
        }
        .stat-card.success { background: linear-gradient(135deg, #84fab0 0%, #8fd3f4 100%); }
        .stat-card.danger { background: linear-gradient(135deg, #ff9a9e 0%, #fecfef 100%); }
        .stat-value { font-size: 3em; font-weight: bold; color: #2d3748; margin: 10px 0; }
        .stat-label { font-size: 1.1em; color: #4a5568; text-transform: uppercase; }
        .coverage-bar {
            background: #e2e8f0;
            border-radius: 10px;
            height: 30px;
            margin: 10px 0;
            overflow: hidden;
            position: relative;
        }
        .coverage-fill {
            background: linear-gradient(90deg, #667eea 0%, #764ba2 100%);
            height: 100%;
            border-radius: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-weight: bold;
        }
        .test-class-card {
            background: white;
            border: 2px solid #e2e8f0;
            border-radius: 10px;
            padding: 20px;
            margin-bottom: 15px;
        }
        .footer {
            background: #2d3748;
            color: white;
            padding: 20px;
            text-align: center;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Test Istatistikleri</h1>
            <p>Plafind Test Raporu</p>
        </div>
        <div class="content">
            <div class="stats-grid">
                <div class="stat-card success">
                    <div class="stat-label">Toplam Test</div>
                    <div class="stat-value">$total</div>
                </div>
                <div class="stat-card success">
                    <div class="stat-label">Basarili</div>
                    <div class="stat-value">$passed</div>
                </div>
                <div class="stat-card $(if ($failed -gt 0) { 'danger' } else { 'success' })">
                    <div class="stat-label">Basarisiz</div>
                    <div class="stat-value">$failed</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Basar Orani</div>
                    <div class="stat-value">$successRate%</div>
                </div>
            </div>
"@

if ($Coverage -and (Test-Path "./TestResults/coverage.json")) {
    try {
        $cov = Get-Content "./TestResults/coverage.json" -Raw | ConvertFrom-Json
        $lineCov = [math]::Round($cov.summary.linecoverage, 2)
        $branchCov = [math]::Round($cov.summary.branchcoverage, 2)
        $htmlContent += @"
            <div style="background: #f7fafc; padding: 25px; border-radius: 10px; margin-bottom: 30px;">
                <h2 style="margin-bottom: 20px; color: #2d3748;">Code Coverage</h2>
                <div>
                    <strong>Line Coverage:</strong>
                    <div class="coverage-bar">
                        <div class="coverage-fill" style="width: $lineCov%">$lineCov%</div>
                    </div>
                </div>
                <div>
                    <strong>Branch Coverage:</strong>
                    <div class="coverage-bar">
                        <div class="coverage-fill" style="width: $branchCov%">$branchCov%</div>
                    </div>
                </div>
            </div>
"@
    } catch { }
}

$htmlContent += @"
            <div>
                <h2 style="margin-bottom: 20px; color: #2d3748;">Test Siniflari</h2>
"@

foreach ($detail in $classDetails | Sort-Object Category, Class) {
    $htmlContent += @"
                <div class="test-class-card">
                    <div style="font-size: 1.3em; font-weight: bold; color: #2d3748; margin-bottom: 10px;">
                        [$($detail.Category)] $($detail.Class)
                    </div>
                    <div style="color: #718096; font-size: 0.9em;">
                        $($detail.Methods) test metodu - $($detail.File)
                    </div>
                </div>
"@
}

$htmlContent += @"
            </div>
        </div>
        <div class="footer">
            <div style="color: #a0aec0; font-size: 0.9em;">
                Rapor Olusturulma Tarihi: $(Get-Date -Format "dd.MM.yyyy HH:mm:ss")
            </div>
        </div>
    </div>
</body>
</html>
"@

$htmlContent | Out-File -FilePath $htmlPath -Encoding UTF8

Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "HTML raporu olusturuldu: $htmlPath" -ForegroundColor Green
Write-Host ""

if ($OpenHtml) {
    Start-Process $htmlPath
}

