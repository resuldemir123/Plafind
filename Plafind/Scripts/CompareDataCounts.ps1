# Veri Sayılarını Karşılaştırma Scripti
# Bu script SQL Server ve MySQL'deki veri sayılarını karşılaştırır

param(
    [string]$SqlServerConnection = "Server=(localdb)\MSSQLLocalDB;Database=AlanyaBusinessGuide;Trusted_Connection=True;",
    [string]$MySqlConnection = "Server=localhost;Database=AlanyaBusinessGuide;User=root;Password=resulfe123;Port=3306;"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Veri Sayıları Karşılaştırma" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$tables = @(
    "AspNetUsers",
    "AspNetRoles",
    "Businesses",
    "Categories",
    "Reviews",
    "Reservations",
    "News",
    "UserFavorites",
    "UserProfiles",
    "Payments",
    "Subscriptions",
    "Notifications",
    "Messages",
    "Events",
    "Campaigns",
    "Branches",
    "Employees",
    "BusinessImages",
    "ReviewReplies",
    "ReviewLikes",
    "ContactMessages",
    "AdminLogs"
)

Write-Host "SQL Server Veri Sayıları:" -ForegroundColor Yellow
Write-Host "------------------------" -ForegroundColor Yellow

try {
    $sqlServerCounts = @{}
    foreach ($table in $tables) {
        try {
            $query = "SELECT COUNT(*) AS Count FROM [$table]"
            # SQL Server sorgusu burada çalıştırılacak
            # Not: Bu script için SQL Server bağlantısı kurmanız gerekebilir
            Write-Host "$table`: [SQL Server bağlantısı gerekli]" -ForegroundColor Gray
        } catch {
            Write-Host "$table`: Hata - $_" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "SQL Server bağlantısı kurulamadı: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "MySQL Veri Sayıları:" -ForegroundColor Yellow
Write-Host "-------------------" -ForegroundColor Yellow

try {
    $mySqlCounts = @{}
    foreach ($table in $tables) {
        try {
            $query = "SELECT COUNT(*) AS Count FROM `$table`"
            # MySQL sorgusu burada çalıştırılacak
            # Not: Bu script için MySQL bağlantısı kurmanız gerekebilir
            Write-Host "$table`: [MySQL bağlantısı gerekli]" -ForegroundColor Gray
        } catch {
            Write-Host "$table`: Hata - $_" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "MySQL bağlantısı kurulamadı: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "NOT: Bu script sadece bir şablon." -ForegroundColor Yellow
Write-Host "Gerçek veri kontrolü için SQL sorgularını kullanın:" -ForegroundColor Yellow
Write-Host "1. CheckDataCounts.sql (SQL Server için)" -ForegroundColor White
Write-Host "2. CheckMySQLDataCounts.sql (MySQL için)" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
