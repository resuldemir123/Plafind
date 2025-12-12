-- LocalDB'den Veri Export Scripti
-- Bu scripti Visual Studio SQL Server Object Explorer'da veya SSMS'de çalıştırın
-- Connection: (localdb)\MSSQLLocalDB

USE AlanyaBusinessGuide;
GO

-- Tüm tablolardaki veri sayılarını göster
SELECT 'AspNetUsers' AS TableName, COUNT(*) AS RecordCount FROM AspNetUsers
UNION ALL
SELECT 'AspNetRoles', COUNT(*) FROM AspNetRoles
UNION ALL
SELECT 'Businesses', COUNT(*) FROM Businesses
UNION ALL
SELECT 'Categories', COUNT(*) FROM Categories
UNION ALL
SELECT 'Reviews', COUNT(*) FROM Reviews
UNION ALL
SELECT 'Reservations', COUNT(*) FROM Reservations
UNION ALL
SELECT 'News', COUNT(*) FROM News
UNION ALL
SELECT 'UserFavorites', COUNT(*) FROM UserFavorites
UNION ALL
SELECT 'UserProfiles', COUNT(*) FROM UserProfiles
UNION ALL
SELECT 'Payments', COUNT(*) FROM Payments
UNION ALL
SELECT 'Subscriptions', COUNT(*) FROM Subscriptions
UNION ALL
SELECT 'Notifications', COUNT(*) FROM Notifications
UNION ALL
SELECT 'Messages', COUNT(*) FROM Messages
UNION ALL
SELECT 'Events', COUNT(*) FROM Events
UNION ALL
SELECT 'Campaigns', COUNT(*) FROM Campaigns
UNION ALL
SELECT 'Branches', COUNT(*) FROM Branches
UNION ALL
SELECT 'Employees', COUNT(*) FROM Employees
UNION ALL
SELECT 'BusinessImages', COUNT(*) FROM BusinessImages
UNION ALL
SELECT 'ReviewReplies', COUNT(*) FROM ReviewReplies
UNION ALL
SELECT 'ReviewLikes', COUNT(*) FROM ReviewLikes
UNION ALL
SELECT 'ContactMessages', COUNT(*) FROM ContactMessages
UNION ALL
SELECT 'AdminLogs', COUNT(*) FROM AdminLogs
ORDER BY TableName;
GO

-- Her tablodan verileri göster (ilk 10 kayıt)
-- Bu sorguları çalıştırıp sonuçları CSV olarak kaydedebilirsiniz

-- Kategoriler
SELECT TOP 10 * FROM Categories;
GO

-- İşletmeler
SELECT TOP 10 * FROM Businesses;
GO

-- Kullanıcılar (şifreler hariç)
SELECT Id, UserName, Email, EmailConfirmed, PhoneNumber FROM AspNetUsers;
GO

-- Yorumlar
SELECT TOP 10 * FROM Reviews;
GO

-- Rezervasyonlar
SELECT TOP 10 * FROM Reservations;
GO

-- NOT: Sonuçları kaydetmek için:
-- 1. Query sonuçlarına sağ tıklayın
-- 2. "Save Results As..." seçin
-- 3. CSV formatında kaydedin
-- 4. MySQL Workbench'te import edin
