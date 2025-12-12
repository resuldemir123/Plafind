# SQL Server'dan MySQL'e Veri Taşıma Scripti
# Kullanım: .\MigrateToMySQL.ps1

param(
    [string]$SqlServerConnection = "Server=(localdb)\MSSQLLocalDB;Database=AlanyaBusinessGuide;Trusted_Connection=True;",
    [string]$MySqlServer = "localhost",
    [string]$MySqlDatabase = "AlanyaBusinessGuide",
    [string]$MySqlUser = "root",
    [string]$MySqlPassword = "",
    [int]$MySqlPort = 3306
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SQL Server -> MySQL Veri Taşıma Aracı" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# MySQL bağlantı string'i oluştur
$MySqlConnection = "Server=$MySqlServer;Database=$MySqlDatabase;User=$MySqlUser;Password=$MySqlPassword;Port=$MySqlPort;CharSet=utf8mb4;"

Write-Host "SQL Server Bağlantısı: $SqlServerConnection" -ForegroundColor Yellow
Write-Host "MySQL Bağlantısı: $MySqlServer:$MySqlPort/$MySqlDatabase" -ForegroundColor Yellow
Write-Host ""

# MySQL bağlantısını test et
try {
    Add-Type -Path "C:\Program Files (x86)\MySQL\MySQL Connector Net 8.0.33\Assemblies\v4.5.2\MySql.Data.dll" -ErrorAction SilentlyContinue
    $mysqlConn = New-Object MySql.Data.MySqlClient.MySqlConnection($MySqlConnection)
    $mysqlConn.Open()
    Write-Host "✓ MySQL bağlantısı başarılı!" -ForegroundColor Green
    $mysqlConn.Close()
} catch {
    Write-Host "✗ MySQL bağlantısı başarısız: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "MySQL Connector/NET kurulu değil veya bağlantı bilgileri hatalı." -ForegroundColor Yellow
    Write-Host "Alternatif: C# migration scriptini kullanın veya MySQL Workbench ile manuel taşıma yapın." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "NOT: Bu PowerShell scripti basit bir örnektir." -ForegroundColor Yellow
Write-Host "Gerçek veri taşıma için aşağıdaki yöntemlerden birini kullanın:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. MySQL Workbench Migration Wizard kullanın" -ForegroundColor Cyan
Write-Host "2. C# migration scriptini çalıştırın (dotnet run)" -ForegroundColor Cyan
Write-Host "3. SQL Server Management Studio -> MySQL Workbench export/import" -ForegroundColor Cyan
Write-Host ""
