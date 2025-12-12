-- SQL Server'daki Veri Sayılarını Kontrol Etme Scripti
-- Bu scripti SQL Server Management Studio'da çalıştırın

USE AlanyaBusinessGuide;
GO

SELECT 'SQL Server Veri Sayıları' AS DatabaseType;

-- Tablolar ve kayıt sayıları
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

-- Toplam kayıt sayısı
SELECT 'TOPLAM' AS TableName, 
       (SELECT COUNT(*) FROM AspNetUsers) +
       (SELECT COUNT(*) FROM AspNetRoles) +
       (SELECT COUNT(*) FROM Businesses) +
       (SELECT COUNT(*) FROM Categories) +
       (SELECT COUNT(*) FROM Reviews) +
       (SELECT COUNT(*) FROM Reservations) +
       (SELECT COUNT(*) FROM News) +
       (SELECT COUNT(*) FROM UserFavorites) +
       (SELECT COUNT(*) FROM UserProfiles) +
       (SELECT COUNT(*) FROM Payments) +
       (SELECT COUNT(*) FROM Subscriptions) +
       (SELECT COUNT(*) FROM Notifications) +
       (SELECT COUNT(*) FROM Messages) +
       (SELECT COUNT(*) FROM Events) +
       (SELECT COUNT(*) FROM Campaigns) +
       (SELECT COUNT(*) FROM Branches) +
       (SELECT COUNT(*) FROM Employees) +
       (SELECT COUNT(*) FROM BusinessImages) +
       (SELECT COUNT(*) FROM ReviewReplies) +
       (SELECT COUNT(*) FROM ReviewLikes) +
       (SELECT COUNT(*) FROM ContactMessages) +
       (SELECT COUNT(*) FROM AdminLogs) AS RecordCount;
