-- SQL Server'dan Veri Export Scripti
-- Bu scripti SQL Server Management Studio'da çalıştırın
-- Sonuçları CSV veya SQL dosyası olarak kaydedin

USE AlanyaBusinessGuide;
GO

-- 1. Kullanıcılar (AspNetUsers)
SELECT * FROM AspNetUsers;
GO

-- 2. Roller (AspNetRoles)
SELECT * FROM AspNetRoles;
GO

-- 3. Kullanıcı Rolleri (AspNetUserRoles)
SELECT * FROM AspNetUserRoles;
GO

-- 4. Kategoriler
SELECT * FROM Categories;
GO

-- 5. İşletmeler
SELECT * FROM Businesses;
GO

-- 6. Yorumlar
SELECT * FROM Reviews;
GO

-- 7. Rezervasyonlar
SELECT * FROM Reservations;
GO

-- 8. Haberler
SELECT * FROM News;
GO

-- 9. Favoriler
SELECT * FROM UserFavorites;
GO

-- 10. Kullanıcı Profilleri
SELECT * FROM UserProfiles;
GO

-- 11. Ödemeler
SELECT * FROM Payments;
GO

-- 12. Abonelikler
SELECT * FROM Subscriptions;
GO

-- 13. Bildirimler
SELECT * FROM Notifications;
GO

-- 14. Mesajlar
SELECT * FROM Messages;
GO

-- 15. Etkinlikler
SELECT * FROM Events;
GO

-- 16. Kampanyalar
SELECT * FROM Campaigns;
GO

-- 17. Şubeler
SELECT * FROM Branches;
GO

-- 18. Çalışanlar
SELECT * FROM Employees;
GO

-- 19. İşletme Görselleri
SELECT * FROM BusinessImages;
GO

-- 20. Yorum Yanıtları
SELECT * FROM ReviewReplies;
GO

-- 21. Yorum Beğenileri
SELECT * FROM ReviewLikes;
GO

-- 22. İletişim Mesajları
SELECT * FROM ContactMessages;
GO

-- 23. Admin Logları
SELECT * FROM AdminLogs;
GO

-- NOT: Her sorgunun sonuçlarını:
-- 1. Right-click > "Save Results As..." ile CSV olarak kaydedin
-- 2. Veya "Results to File" ile SQL dosyası olarak kaydedin
