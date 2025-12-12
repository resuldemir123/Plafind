-- MySQL'e Veri Import Scripti
-- Bu scripti MySQL Workbench'te çalıştırın
-- ÖNCE: SQL Server'dan export edilen verileri hazırlayın

USE AlanyaBusinessGuide;

-- Foreign key kontrollerini geçici olarak kapat
SET FOREIGN_KEY_CHECKS = 0;

-- 1. Kategoriler (önce bunlar gelmeli - foreign key bağımlılığı var)
-- INSERT INTO Categories (Id, Name, Description, CreatedDate) VALUES (...);

-- 2. Roller (AspNetRoles)
-- INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (...);

-- 3. Kullanıcılar (AspNetUsers)
-- INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, ...) VALUES (...);

-- 4. Kullanıcı Rolleri (AspNetUserRoles)
-- INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (...);

-- 5. İşletmeler
-- INSERT INTO Businesses (Id, Name, Description, CategoryId, ...) VALUES (...);

-- 6. Yorumlar
-- INSERT INTO Reviews (Id, BusinessId, UserId, Rating, Comment, ...) VALUES (...);

-- 7. Rezervasyonlar
-- INSERT INTO Reservations (Id, BusinessId, UserId, ReservationDate, ...) VALUES (...);

-- 8. Haberler
-- INSERT INTO News (Id, Title, Content, AuthorId, ...) VALUES (...);

-- 9. Favoriler
-- INSERT INTO UserFavorites (Id, UserId, BusinessId, CreatedDate) VALUES (...);

-- 10. Kullanıcı Profilleri
-- INSERT INTO UserProfiles (Id, UserId, FirstName, LastName, ...) VALUES (...);

-- 11. Ödemeler
-- INSERT INTO Payments (Id, UserId, BusinessId, Amount, ...) VALUES (...);

-- 12. Abonelikler
-- INSERT INTO Subscriptions (Id, UserId, BusinessId, StartDate, ...) VALUES (...);

-- 13. Bildirimler
-- INSERT INTO Notifications (Id, UserId, Title, Message, ...) VALUES (...);

-- 14. Mesajlar
-- INSERT INTO Messages (Id, SenderId, ReceiverId, Subject, ...) VALUES (...);

-- 15. Etkinlikler
-- INSERT INTO Events (Id, BusinessId, Title, Description, ...) VALUES (...);

-- 16. Kampanyalar
-- INSERT INTO Campaigns (Id, BusinessId, Title, Description, ...) VALUES (...);

-- 17. Şubeler
-- INSERT INTO Branches (Id, BusinessId, Name, Address, ...) VALUES (...);

-- 18. Çalışanlar
-- INSERT INTO Employees (Id, BusinessId, BranchId, Name, ...) VALUES (...);

-- 19. İşletme Görselleri
-- INSERT INTO BusinessImages (Id, BusinessId, ImageUrl, ...) VALUES (...);

-- 20. Yorum Yanıtları
-- INSERT INTO ReviewReplies (Id, ReviewId, UserId, ReplyText, ...) VALUES (...);

-- 21. Yorum Beğenileri
-- INSERT INTO ReviewLikes (Id, ReviewId, UserId, IsLike, ...) VALUES (...);

-- 22. İletişim Mesajları
-- INSERT INTO ContactMessages (Id, Name, Email, Subject, ...) VALUES (...);

-- 23. Admin Logları
-- INSERT INTO AdminLogs (Id, AdminUserId, Action, ...) VALUES (...);

-- Foreign key kontrollerini tekrar aç
SET FOREIGN_KEY_CHECKS = 1;

-- NOT: Bu script bir şablondur. 
-- Gerçek verileri SQL Server'dan export edip buraya INSERT komutları olarak eklemeniz gerekir.
