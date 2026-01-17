# Test İstatistiksel Rapor Oluşturma Scripti
param(
    [switch]$Coverage,
    [switch]$Detailed
)

Write-Host "🧪 Test İstatistiksel Raporu Oluşturuluyor..." -ForegroundColor Cyan
Write-Host ""

# Test sonuçlarını al
$testOutput = dotnet test --logger "console;verbosity=normal" 2>&1

# Test istatistiklerini çıkar
$totalTests = 0
$passedTests = 0
$failedTests = 0
$skippedTests = 0
$testDuration = 0

# Test sonuçlarını parse et
if ($testOutput -match "Toplam test sayısı:\s*(\d+)") {
    $totalTests = [int]$matches[1]
}
if ($testOutput -match "Geçti:\s*(\d+)") {
    $passedTests = [int]$matches[1]
}
if ($testOutput -match "Başarısız:\s*(\d+)") {
    $failedTests = [int]$matches[1]
}
if ($testOutput -match "Atlandı:\s*(\d+)") {
    $skippedTests = [int]$matches[1]
}
if ($testOutput -match "Toplam süre:\s*([\d,\.]+)\s*Saniye") {
    $testDuration = [double]$matches[1].Replace(",", ".")
}

# Coverage raporu oluştur
if ($Coverage) {
    Write-Host "📊 Coverage raporu oluşturuluyor..." -ForegroundColor Yellow
    dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover,json /p:CoverletOutput=./TestResults/coverage --results-directory ./TestResults | Out-Null
    
    if (Test-Path "./TestResults/coverage.json") {
        $coverageData = Get-Content "./TestResults/coverage.json" | ConvertFrom-Json
        $lineCoverage = $coverageData.summary.linecoverage
        $branchCoverage = $coverageData.summary.branchcoverage
    }
}

# Test sınıflarını analiz et
$testFiles = Get-ChildItem -Path . -Filter "*Tests.cs" -Recurse
$testClasses = @()
$testMethods = @()

foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    $className = $file.BaseName
    
    # Test metodlarını bul
    $methodMatches = [regex]::Matches($content, '\[Fact\]|\[Theory\]')
    $methodCount = $methodMatches.Count
    
    $testClasses += @{
        Name = $className
        File = $file.Name
        MethodCount = $methodCount
    }
    
    $testMethods += $methodCount
}

# HTML raporu oluştur
$htmlReport = @"
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Plafind Test İstatistikleri</title>
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
        .header h1 {
            font-size: 2.5em;
            margin-bottom: 10px;
        }
        .header p {
            font-size: 1.1em;
            opacity: 0.9;
        }
        .content {
            padding: 30px;
        }
        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }
        .stat-card {
            background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
            padding: 25px;
            border-radius: 10px;
            text-align: center;
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
            transition: transform 0.3s;
        }
        .stat-card:hover {
            transform: translateY(-5px);
        }
        .stat-card.success { background: linear-gradient(135deg, #84fab0 0%, #8fd3f4 100%); }
        .stat-card.warning { background: linear-gradient(135deg, #ffecd2 0%, #fcb69f 100%); }
        .stat-card.danger { background: linear-gradient(135deg, #ff9a9e 0%, #fecfef 100%); }
        .stat-card.info { background: linear-gradient(135deg, #a8edea 0%, #fed6e3 100%); }
        .stat-value {
            font-size: 3em;
            font-weight: bold;
            color: #2d3748;
            margin: 10px 0;
        }
        .stat-label {
            font-size: 1.1em;
            color: #4a5568;
            text-transform: uppercase;
            letter-spacing: 1px;
        }
        .coverage-section {
            background: #f7fafc;
            padding: 25px;
            border-radius: 10px;
            margin-bottom: 30px;
        }
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
            transition: width 1s;
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-weight: bold;
        }
        .test-classes {
            margin-top: 30px;
        }
        .test-class-card {
            background: white;
            border: 2px solid #e2e8f0;
            border-radius: 10px;
            padding: 20px;
            margin-bottom: 15px;
            transition: all 0.3s;
        }
        .test-class-card:hover {
            border-color: #667eea;
            box-shadow: 0 5px 15px rgba(102, 126, 234, 0.2);
        }
        .test-class-name {
            font-size: 1.3em;
            font-weight: bold;
            color: #2d3748;
            margin-bottom: 10px;
        }
        .test-class-info {
            color: #718096;
            font-size: 0.9em;
        }
        .footer {
            background: #2d3748;
            color: white;
            padding: 20px;
            text-align: center;
        }
        .timestamp {
            color: #a0aec0;
            font-size: 0.9em;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>📊 Test İstatistikleri</h1>
            <p>Plafind Test Raporu</p>
        </div>
        <div class="content">
            <div class="stats-grid">
                <div class="stat-card success">
                    <div class="stat-label">Toplam Test</div>
                    <div class="stat-value">$totalTests</div>
                </div>
                <div class="stat-card success">
                    <div class="stat-label">Başarılı</div>
                    <div class="stat-value">$passedTests</div>
                </div>
                <div class="stat-card danger">
                    <div class="stat-label">Başarısız</div>
                    <div class="stat-value">$failedTests</div>
                </div>
                <div class="stat-card info">
                    <div class="stat-label">Süre (sn)</div>
                    <div class="stat-value">$([math]::Round($testDuration, 2))</div>
                </div>
            </div>
"@

if ($Coverage -and $lineCoverage) {
    $htmlReport += @"
            <div class="coverage-section">
                <h2 style="margin-bottom: 20px; color: #2d3748;">📈 Code Coverage</h2>
                <div>
                    <strong>Line Coverage:</strong>
                    <div class="coverage-bar">
                        <div class="coverage-fill" style="width: $($lineCoverage)%">$([math]::Round($lineCoverage, 2))%</div>
                    </div>
                </div>
                <div>
                    <strong>Branch Coverage:</strong>
                    <div class="coverage-bar">
                        <div class="coverage-fill" style="width: $($branchCoverage)%">$([math]::Round($branchCoverage, 2))%</div>
                    </div>
                </div>
            </div>
"@
}

$htmlReport += @"
            <div class="test-classes">
                <h2 style="margin-bottom: 20px; color: #2d3748;">📁 Test Sınıfları</h2>
"@

foreach ($testClass in $testClasses) {
    $htmlReport += @"
                <div class="test-class-card">
                    <div class="test-class-name">$($testClass.Name)</div>
                    <div class="test-class-info">
                        📄 Dosya: $($testClass.File) | 🧪 Test Metodu: $($testClass.MethodCount)
                    </div>
                </div>
"@
}

$htmlReport += @"
            </div>
        </div>
        <div class="footer">
            <div class="timestamp">Rapor Oluşturulma Tarihi: $(Get-Date -Format "dd.MM.yyyy HH:mm:ss")</div>
        </div>
    </div>
</body>
</html>
"@

# Raporu kaydet
$reportPath = "./TestResults/test-report.html"
$htmlReport | Out-File -FilePath $reportPath -Encoding UTF8

Write-Host "✅ Test raporu oluşturuldu: $reportPath" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Test İstatistikleri:" -ForegroundColor Cyan
Write-Host "   Toplam Test: $totalTests" -ForegroundColor White
Write-Host "   Başarılı: $passedTests" -ForegroundColor Green
Write-Host "   Başarısız: $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Green" })
Write-Host "   Atlanan: $skippedTests" -ForegroundColor Yellow
Write-Host "   Süre: $([math]::Round($testDuration, 2)) saniye" -ForegroundColor White
Write-Host ""
Write-Host "📁 Test Sınıfları:" -ForegroundColor Cyan
foreach ($testClass in $testClasses) {
    Write-Host "   - $($testClass.Name): $($testClass.MethodCount) test metodu" -ForegroundColor White
}

if ($Coverage -and $lineCoverage) {
    Write-Host ""
    Write-Host "📈 Code Coverage:" -ForegroundColor Cyan
    Write-Host "   Line Coverage: $([math]::Round($lineCoverage, 2))%" -ForegroundColor White
    Write-Host "   Branch Coverage: $([math]::Round($branchCoverage, 2))%" -ForegroundColor White
}

Write-Host ""
Write-Host "🌐 HTML raporunu görüntülemek için:" -ForegroundColor Yellow
Write-Host "   Start-Process '$reportPath'" -ForegroundColor White

