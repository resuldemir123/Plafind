using Plafind.Models;
using Microsoft.EntityFrameworkCore;

namespace Plafind.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDataAsync(ApplicationDbContext context)
        {
            // Kategori kontrolü ve ekleme
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Restoran", Description = "Yemek ve içecek hizmetleri" },
                    new Category { Name = "Otel", Description = "Konaklama hizmetleri" },
                    new Category { Name = "Mağaza", Description = "Alışveriş merkezleri" },
                    new Category { Name = "Spa & Wellness", Description = "Sağlık ve güzellik" },
                    new Category { Name = "Eğlence", Description = "Eğlence ve aktiviteler" },
                    new Category { Name = "Sağlık", Description = "Sağlık hizmetleri" },
                    new Category { Name = "Eğitim", Description = "Eğitim kurumları" },
                    new Category { Name = "Kafe", Description = "Kafe ve pastane" }
                };
                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // İşletme kontrolü ve ekleme (her zaman 10 adet ekle)
            var businessCount = await context.Businesses.CountAsync();
            if (businessCount < 10)
            {
                var categories = await context.Categories.ToListAsync();
                var random = new Random();

                var businesses = new List<Business>();

                // Restoranlar (15 adet)
                var restaurantCategory = categories.FirstOrDefault(c => c.Name == "Restoran");
                if (restaurantCategory != null)
                {
                    businesses.AddRange(new[]
                    {
                        new Business { Name = "Alanya Balık Evi", Address = "Keykubat Bulvarı No:123 Alanya", Phone = "+90 242 513 1234", CategoryId = restaurantCategory.Id, Description = "Taze deniz mahsulleri ve muhteşem manzara", Email = "info@alanyabalik.com", Website = "www.alanyabalik.com", WorkingHours = "11:00-23:00", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=800", AverageRating = 4.5, TotalReviews = 125 },
                        new Business { Name = "Sultan Sofrası", Address = "Saray Mahallesi Atatürk Caddesi No:45", Phone = "+90 242 513 2345", CategoryId = restaurantCategory.Id, Description = "Geleneksel Türk mutfağı ve kebap çeşitleri", Email = "info@sultansofrasi.com", Website = "www.sultansofrasi.com", WorkingHours = "10:00-00:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=800", AverageRating = 4.3, TotalReviews = 98 },
                        new Business { Name = "Manzara Restaurant", Address = "Kaleiçi Yat Limanı No:7", Phone = "+90 242 513 3456", CategoryId = restaurantCategory.Id, Description = "Deniz manzaralı romantik akşam yemekleri", Email = "rezervasyon@manzara.com", Website = "www.manzararestaurant.com", WorkingHours = "12:00-24:00", PriceRange = "₺₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=800", AverageRating = 4.8, TotalReviews = 210 },
                        new Business { Name = "Pizza Palace", Address = "Saray Mahallesi Gazi Caddesi No:89", Phone = "+90 242 513 4567", CategoryId = restaurantCategory.Id, Description = "İtalyan mutfağı ve fırında pişmiş pizza", Email = "info@pizzapalace.com", Website = "www.pizzapalace.com", WorkingHours = "11:30-23:30", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=800", AverageRating = 4.2, TotalReviews = 87 },
                        new Business { Name = "Garden Restaurant", Address = "Oba Mahallesi No:156", Phone = "+90 242 513 5678", CategoryId = restaurantCategory.Id, Description = "Bahçe konseptinde aile restoranı", Email = "info@garden.com", Website = "www.gardenrestaurant.com", WorkingHours = "09:00-23:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1552566626-52f8b828add9?w=800", AverageRating = 4.0, TotalReviews = 65 },
                        new Business { Name = "Meze House", Address = "Damlataş Caddesi No:34", Phone = "+90 242 513 6789", CategoryId = restaurantCategory.Id, Description = "Ege mezeler ve rakı balık keyfi", Email = "info@mezehouse.com", Website = "www.mezehouse.com", WorkingHours = "17:00-02:00", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1544148103-0773bf10d330?w=800", AverageRating = 4.6, TotalReviews = 143 },
                        new Business { Name = "Steak House Alanya", Address = "Kızlar Pınarı Mevkii No:23", Phone = "+90 242 513 7890", CategoryId = restaurantCategory.Id, Description = "Premium et çeşitleri ve şarap seçenekleri", Email = "rezervasyon@steakhouse.com", Website = "www.steakhousealanya.com", WorkingHours = "18:00-01:00", PriceRange = "₺₺₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800", AverageRating = 4.7, TotalReviews = 156 },
                        new Business { Name = "Sushi Bar Alanya", Address = "Mahmutlar Mahallesi No:78", Phone = "+90 242 513 8901", CategoryId = restaurantCategory.Id, Description = "Japon mutfağı ve sushi çeşitleri", Email = "info@sushibar.com", Website = "www.sushibaralanya.com", WorkingHours = "12:00-23:00", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1579584425555-c3ce17fd4351?w=800", AverageRating = 4.4, TotalReviews = 92 },
                        new Business { Name = "Mangal Keyfi", Address = "Güller Pınarı Mahallesi No:45", Phone = "+90 242 513 9012", CategoryId = restaurantCategory.Id, Description = "Izgara ve mangal lezzetleri", Email = "info@mangalkeyfi.com", Website = "www.mangalkeyfi.com", WorkingHours = "11:00-23:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1529042410759-befb1204b468?w=800", AverageRating = 4.1, TotalReviews = 78 },
                        new Business { Name = "Deniz Kızı Restaurant", Address = "Saray Sahil No:12", Phone = "+90 242 514 0123", CategoryId = restaurantCategory.Id, Description = "Kumsal kenarında taze deniz ürünleri", Email = "info@denizkizi.com", Website = "www.denizkirestaurant.com", WorkingHours = "10:00-23:00", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1550966871-3ed3cdb5ed0c?w=800", AverageRating = 4.5, TotalReviews = 134 },
                        new Business { Name = "Vegan Corner", Address = "Cikcilli Mahallesi No:67", Phone = "+90 242 514 1234", CategoryId = restaurantCategory.Id, Description = "Sağlıklı vegan ve vejetaryen yemekler", Email = "info@vegancorner.com", Website = "www.vegancorner.com", WorkingHours = "09:00-22:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=800", AverageRating = 4.3, TotalReviews = 89 },
                        new Business { Name = "Breakfast Club", Address = "Oba Mahallesi Atatürk Caddesi No:89", Phone = "+90 242 514 2345", CategoryId = restaurantCategory.Id, Description = "Kahvaltı ve brunch çeşitleri", Email = "info@breakfastclub.com", Website = "www.breakfastclub.com", WorkingHours = "07:00-15:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1533777324565-a040eb52facd?w=800", AverageRating = 4.6, TotalReviews = 167 },
                        new Business { Name = "Teras Restaurant", Address = "Kleopatra Plajı No:45", Phone = "+90 242 514 3456", CategoryId = restaurantCategory.Id, Description = "Teras katında deniz manzaralı yemekler", Email = "info@terasrestaurant.com", Website = "www.terasrestaurant.com", WorkingHours = "11:00-01:00", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1559339352-11d035aa65de?w=800", AverageRating = 4.7, TotalReviews = 178 },
                        new Business { Name = "Burger Town", Address = "Mahmutlar Antalya Yolu No:123", Phone = "+90 242 514 4567", CategoryId = restaurantCategory.Id, Description = "Gurme burger ve fast food", Email = "info@burgertown.com", Website = "www.burgertown.com", WorkingHours = "11:00-23:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800", AverageRating = 4.0, TotalReviews = 72 },
                        new Business { Name = "Köy Sofrası", Address = "Tosmur Mahallesi No:34", Phone = "+90 242 514 5678", CategoryId = restaurantCategory.Id, Description = "Ev yapımı köy yemekleri ve organik ürünler", Email = "info@koysofrasi.com", Website = "www.koysofrasi.com", WorkingHours = "10:00-22:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=800", AverageRating = 4.4, TotalReviews = 103 }
                    });
                }

                // Oteller (12 adet)
                var hotelCategory = categories.FirstOrDefault(c => c.Name == "Otel");
                if (hotelCategory != null)
                {
                    businesses.AddRange(new[]
                    {
                        new Business { Name = "Grand Paradise Hotel", Address = "Okurcalar Kasabası Alanya", Phone = "+90 242 527 1000", CategoryId = hotelCategory.Id, Description = "5 yıldızlı ultra her şey dahil otel", Email = "info@grandparadise.com", Website = "www.grandparadise.com", WorkingHours = "7/24", PriceRange = "₺₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=800", AverageRating = 4.8, TotalReviews = 542 },
                        new Business { Name = "Alanya Beach Resort", Address = "Kleopatra Plajı Keykubat Bulvarı", Phone = "+90 242 527 2000", CategoryId = hotelCategory.Id, Description = "Plaj kenarında lüks konaklama", Email = "rezervasyon@alanyabeach.com", Website = "www.alanyabeachresort.com", WorkingHours = "7/24", PriceRange = "₺₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=800", AverageRating = 4.6, TotalReviews = 387 },
                        new Business { Name = "City Hotel Alanya", Address = "Saray Mahallesi Merkez", Phone = "+90 242 527 3000", CategoryId = hotelCategory.Id, Description = "Şehir merkezinde butik otel", Email = "info@cityhotel.com", Website = "www.cityhote lalanya.com", WorkingHours = "7/24", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1564501049412-61c2a3083791?w=800", AverageRating = 4.2, TotalReviews = 198 },
                        new Business { Name = "Sunset Paradise", Address = "Mahmutlar Sahil No:45", Phone = "+90 242 527 4000", CategoryId = hotelCategory.Id, Description = "Gün batımı manzaralı tatil köyü", Email = "info@sunsetparadise.com", Website = "www.sunsetparadise.com", WorkingHours = "7/24", PriceRange = "₺₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1582719508461-905c673771fd?w=800", AverageRating = 4.7, TotalReviews = 456 },
                        new Business { Name = "Family Hotel", Address = "Oba Mahallesi No:123", Phone = "+90 242 527 5000", CategoryId = hotelCategory.Id, Description = "Aile dostu apart otel", Email = "info@familyhotel.com", Website = "www.familyhotel.com", WorkingHours = "7/24", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?w=800", AverageRating = 4.0, TotalReviews = 143 },
                        new Business { Name = "Spa & Wellness Hotel", Address = "Kızlar Pınarı Mevkii", Phone = "+90 242 527 6000", CategoryId = hotelCategory.Id, Description = "Spa ve wellness merkezi içeren lüks otel", Email = "rezervasyon@spawellness.com", Website = "www.spawellnesshotel.com", WorkingHours = "7/24", PriceRange = "₺₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1540541338287-41700207dee6?w=800", AverageRating = 4.9, TotalReviews = 623 },
                        new Business { Name = "Castle View Hotel", Address = "Kaleiçi Alanya Kalesi Manzara", Phone = "+90 242 527 7000", CategoryId = hotelCategory.Id, Description = "Tarihi kale manzaralı butik otel", Email = "info@castleview.com", Website = "www.castleviewhotel.com", WorkingHours = "7/24", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=800", AverageRating = 4.5, TotalReviews = 234 },
                        new Business { Name = "Budget Inn", Address = "Cikcilli Mahallesi No:67", Phone = "+90 242 527 8000", CategoryId = hotelCategory.Id, Description = "Ekonomik pansiyon ve oda kahvaltı", Email = "info@budgetinn.com", Website = "www.budgetinn.com", WorkingHours = "7/24", PriceRange = "₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1568495248636-6432b97bd949?w=800", AverageRating = 3.8, TotalReviews = 89 },
                        new Business { Name = "Luxury Suites", Address = "Konaklar Mahallesi No:34", Phone = "+90 242 527 9000", CategoryId = hotelCategory.Id, Description = "Apart daireler ve suit odalar", Email = "rezervasyon@luxurysuites.com", Website = "www.luxurysuites.com", WorkingHours = "7/24", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=800", AverageRating = 4.3, TotalReviews = 167 },
                        new Business { Name = "Seaside Resort", Address = "Tosmur Sahil Yolu No:89", Phone = "+90 242 528 0000", CategoryId = hotelCategory.Id, Description = "Deniz kenarında her şey dahil tatil", Email = "info@seasideresort.com", Website = "www.seasideresort.com", WorkingHours = "7/24", PriceRange = "₺₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1602002418082-a4443e081dd1?w=800", AverageRating = 4.6, TotalReviews = 389 },
                        new Business { Name = "Mountain View Hotel", Address = "Güzelbağ Mahallesi Toros Dağları", Phone = "+90 242 528 1000", CategoryId = hotelCategory.Id, Description = "Dağ manzaralı doğa oteli", Email = "info@mountainview.com", Website = "www.mountainviewhotel.com", WorkingHours = "7/24", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1549294413-26f195200c16?w=800", AverageRating = 4.1, TotalReviews = 123 },
                        new Business { Name = "Boutique Palace", Address = "Damlataş Caddesi No:56", Phone = "+90 242 528 2000", CategoryId = hotelCategory.Id, Description = "Özel tasarım butik otel", Email = "rezervasyon@boutiquepalace.com", Website = "www.boutiquepalace.com", WorkingHours = "7/24", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1561501900-3701fa6a0864?w=800", AverageRating = 4.4, TotalReviews = 201 }
                    });
                }

                // Kafeler (8 adet)
                var cafeCategory = categories.FirstOrDefault(c => c.Name == "Kafe");
                if (cafeCategory != null)
                {
                    businesses.AddRange(new[]
                    {
                        new Business { Name = "Coffee Break", Address = "Atatürk Caddesi No:45", Phone = "+90 242 515 1000", CategoryId = cafeCategory.Id, Description = "Özel çekim kahveler ve tatlılar", Email = "info@coffeebreak.com", Website = "www.coffeebreak.com", WorkingHours = "08:00-23:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?w=800", AverageRating = 4.5, TotalReviews = 178 },
                        new Business { Name = "Sweet Corner", Address = "Güller Pınarı Mahallesi No:23", Phone = "+90 242 515 2000", CategoryId = cafeCategory.Id, Description = "Pasta, kurabiye ve tatlı çeşitleri", Email = "info@sweetcorner.com", Website = "www.sweetcorner.com", WorkingHours = "09:00-22:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1559925393-8be0ec4767c8?w=800", AverageRating = 4.3, TotalReviews = 145 },
                        new Business { Name = "Book Cafe", Address = "Cikcilli Mahallesi No:78", Phone = "+90 242 515 3000", CategoryId = cafeCategory.Id, Description = "Kitap okuma ve kahve keyfi", Email = "info@bookcafe.com", Website = "www.bookcafe.com", WorkingHours = "10:00-23:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1519682577862-22b62b24e493?w=800", AverageRating = 4.6, TotalReviews = 203 },
                        new Business { Name = "Latte Art Cafe", Address = "Saray Mahallesi Gazi Caddesi No:12", Phone = "+90 242 515 4000", CategoryId = cafeCategory.Id, Description = "Barista şampiyonasından kahve sanatı", Email = "info@latteartcafe.com", Website = "www.latteartcafe.com", WorkingHours = "07:30-22:30", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1511920170033-f8396924c348?w=800", AverageRating = 4.7, TotalReviews = 189 },
                        new Business { Name = "Garden Cafe", Address = "Oba Mahallesi Bahçe Sokak No:34", Phone = "+90 242 515 5000", CategoryId = cafeCategory.Id, Description = "Bahçe içinde huzurlu ortam", Email = "info@gardencafe.com", Website = "www.gardencafe.com", WorkingHours = "09:00-00:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1554118811-1e0d58224f24?w=800", AverageRating = 4.2, TotalReviews = 134 },
                        new Business { Name = "Sea View Cafe", Address = "Kleopatra Sahil Yolu No:67", Phone = "+90 242 515 6000", CategoryId = cafeCategory.Id, Description = "Deniz manzaralı kahve ve smoothie", Email = "info@seaviewcafe.com", Website = "www.seaviewcafe.com", WorkingHours = "08:00-01:00", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=800", AverageRating = 4.8, TotalReviews = 267 },
                        new Business { Name = "Ice Cream Paradise", Address = "Mahmutlar Merkez No:45", Phone = "+90 242 515 7000", CategoryId = cafeCategory.Id, Description = "El yapımı dondurma ve milkshake", Email = "info@icecreamparadise.com", Website = "www.icecreamparadise.com", WorkingHours = "10:00-23:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1497034825429-c343d7c6a68f?w=800", AverageRating = 4.4, TotalReviews = 156 },
                        new Business { Name = "Waffle House", Address = "Tosmur Mahallesi No:89", Phone = "+90 242 515 8000", CategoryId = cafeCategory.Id, Description = "Waffle, pancake ve brunch", Email = "info@wafflehouse.com", Website = "www.wafflehouse.com", WorkingHours = "08:00-22:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1562376552-0d160a2f238d?w=800", AverageRating = 4.1, TotalReviews = 112 }
                    });
                }

                // Mağazalar (8 adet)
                var shopCategory = categories.FirstOrDefault(c => c.Name == "Mağaza");
                if (shopCategory != null)
                {
                    businesses.AddRange(new[]
                    {
                        new Business { Name = "Alanya AVM", Address = "Saray Mahallesi Merkez", Phone = "+90 242 516 1000", CategoryId = shopCategory.Id, Description = "Alışveriş merkezi ve sinema", Email = "info@alanyaavm.com", Website = "www.alanyaavm.com", WorkingHours = "10:00-22:00", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1555529669-e69e7aa0ba9a?w=800", AverageRating = 4.3, TotalReviews = 234 },
                        new Business { Name = "Fashion Store", Address = "Atatürk Caddesi No:123", Phone = "+90 242 516 2000", CategoryId = shopCategory.Id, Description = "Kadın ve erkek giyim mağazası", Email = "info@fashionstore.com", Website = "www.fashionstore.com", WorkingHours = "09:00-20:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=800", AverageRating = 4.0, TotalReviews = 89 },
                        new Business { Name = "Gold & Silver", Address = "Çarşı İçi No:45", Phone = "+90 242 516 3000", CategoryId = shopCategory.Id, Description = "Altın ve gümüş takı satışı", Email = "info@goldsilver.com", Website = "www.goldsilver.com", WorkingHours = "09:00-19:00", PriceRange = "₺₺₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1515562141207-7a88fb7ce338?w=800", AverageRating = 4.5, TotalReviews = 176 },
                        new Business { Name = "Leather World", Address = "Güller Pınarı No:67", Phone = "+90 242 516 4000", CategoryId = shopCategory.Id, Description = "Deri ceket ve çanta imalatı", Email = "info@leatherworld.com", Website = "www.leatherworld.com", WorkingHours = "09:00-20:00", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1547949003-9792a18a2601?w=800", AverageRating = 4.6, TotalReviews = 198 },
                        new Business { Name = "Carpet Gallery", Address = "Kaleiçi No:23", Phone = "+90 242 516 5000", CategoryId = shopCategory.Id, Description = "El dokuma halı ve kilim", Email = "info@carpetgallery.com", Website = "www.carpetgallery.com", WorkingHours = "08:30-19:30", PriceRange = "₺₺₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=800", AverageRating = 4.2, TotalReviews = 123 },
                        new Business { Name = "Sport Center", Address = "Oba Mahallesi No:89", Phone = "+90 242 516 6000", CategoryId = shopCategory.Id, Description = "Spor malzemeleri ve giyim", Email = "info@sportcenter.com", Website = "www.sportcenter.com", WorkingHours = "09:00-21:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1556906781-9a412961c28c?w=800", AverageRating = 4.1, TotalReviews = 98 },
                        new Business { Name = "Tech Store", Address = "Cikcilli Mahallesi No:34", Phone = "+90 242 516 7000", CategoryId = shopCategory.Id, Description = "Elektronik ve teknoloji ürünleri", Email = "info@techstore.com", Website = "www.techstore.com", WorkingHours = "10:00-20:00", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1498049794561-7780e7231661?w=800", AverageRating = 4.4, TotalReviews = 167 },
                        new Business { Name = "Souvenir Shop", Address = "Damlataş Caddesi No:12", Phone = "+90 242 516 8000", CategoryId = shopCategory.Id, Description = "Hediyelik eşya ve magnet", Email = "info@souvenirshop.com", Website = "www.souvenirshop.com", WorkingHours = "08:00-22:00", PriceRange = "₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1555529902-5261145633bf?w=800", AverageRating = 3.9, TotalReviews = 76 }
                    });
                }

                // Spa & Wellness (7 adet)
                var spaCategory = categories.FirstOrDefault(c => c.Name == "Spa & Wellness");
                if (spaCategory != null)
                {
                    businesses.AddRange(new[]
                    {
                        new Business { Name = "Luxury Spa Center", Address = "Konaklar Mahallesi No:45", Phone = "+90 242 517 1000", CategoryId = spaCategory.Id, Description = "Masaj, hamam ve wellness hizmetleri", Email = "rezervasyon@luxuryspa.com", Website = "www.luxuryspacenter.com", WorkingHours = "09:00-22:00", PriceRange = "₺₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=800", AverageRating = 4.8, TotalReviews = 312 },
                        new Business { Name = "Turkish Bath House", Address = "Saray Mahallesi Hamam Sokak", Phone = "+90 242 517 2000", CategoryId = spaCategory.Id, Description = "Geleneksel Türk hamamı", Email = "info@turkishbath.com", Website = "www.turkishbathhouse.com", WorkingHours = "08:00-23:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1571613327459-5d0935a60f61?w=800", AverageRating = 4.5, TotalReviews = 234 },
                        new Business { Name = "Zen Wellness", Address = "Mahmutlar Merkez No:67", Phone = "+90 242 517 3000", CategoryId = spaCategory.Id, Description = "Yoga, meditasyon ve spa", Email = "info@zenwellness.com", Website = "www.zenwellness.com", WorkingHours = "07:00-21:00", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1544161515-4ab6ce6db874?w=800", AverageRating = 4.7, TotalReviews = 267 },
                        new Business { Name = "Beauty Salon & Spa", Address = "Oba Mahallesi No:89", Phone = "+90 242 517 4000", CategoryId = spaCategory.Id, Description = "Güzellik salonu ve cilt bakımı", Email = "rezervasyon@beautyspa.com", Website = "www.beautysalonspa.com", WorkingHours = "09:00-19:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1560750588-73207b1ef5b8?w=800", AverageRating = 4.3, TotalReviews = 189 },
                        new Business { Name = "Massage Paradise", Address = "Tosmur Sahil Yolu No:23", Phone = "+90 242 517 5000", CategoryId = spaCategory.Id, Description = "Profesyonel masaj terapileri", Email = "info@massageparadise.com", Website = "www.massageparadise.com", WorkingHours = "10:00-22:00", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1519823551278-64ac92734fb1?w=800", AverageRating = 4.6, TotalReviews = 198 },
                        new Business { Name = "Aqua Spa", Address = "Kızlar Pınarı Mevkii No:12", Phone = "+90 242 517 6000", CategoryId = spaCategory.Id, Description = "Su terapisi ve termal havuzlar", Email = "rezervasyon@aquaspa.com", Website = "www.aquaspa.com", WorkingHours = "09:00-21:00", PriceRange = "₺₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1596178060810-4dd9a8a8f3c7?w=800", AverageRating = 4.9, TotalReviews = 423 },
                        new Business { Name = "Fitness & Wellness", Address = "Cikcilli Mahallesi No:34", Phone = "+90 242 517 7000", CategoryId = spaCategory.Id, Description = "Fitness merkezi ve spor salonu", Email = "info@fitnesswellness.com", Website = "www.fitnesswellness.com", WorkingHours = "06:00-23:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1534438327276-14e5300c3a48?w=800", AverageRating = 4.2, TotalReviews = 176 }
                    });
                }

                await context.Businesses.AddRangeAsync(businesses);
                await context.SaveChangesAsync();
            }

            // Eğer hala 10'dan az işletme varsa, daha fazla ekle
            businessCount = await context.Businesses.CountAsync();
            if (businessCount < 10)
            {
                var categories = await context.Categories.ToListAsync();
                var additionalBusinesses = new List<Business>();

                // Ek işletmeler
                var restaurantCategory = categories.FirstOrDefault(c => c.Name == "Restoran");
                var hotelCategory = categories.FirstOrDefault(c => c.Name == "Otel");
                var cafeCategory = categories.FirstOrDefault(c => c.Name == "Kafe");

                if (restaurantCategory != null)
                {
                    additionalBusinesses.AddRange(new[]
                    {
                        new Business { Name = "Garden Restaurant Plus", Address = "Oba Mahallesi Bahçe Sokak No:156", Phone = "+90 242 515 9000", CategoryId = restaurantCategory.Id, Description = "Bahçe konseptinde aile restoranı - Plus versiyonu", Email = "info@gardenplus.com", Website = "www.gardenplus.com", WorkingHours = "09:00-23:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1552566626-52f8b828add9?w=800", AverageRating = 4.2, TotalReviews = 89 },
                        new Business { Name = "Seafood Palace", Address = "Kleopatra Plajı No:78", Phone = "+90 242 515 9100", CategoryId = restaurantCategory.Id, Description = "Taze deniz ürünleri ve balık çeşitleri", Email = "info@seafoodpalace.com", Website = "www.seafoodpalace.com", WorkingHours = "11:00-24:00", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1550966871-3ed3cdb5ed0c?w=800", AverageRating = 4.7, TotalReviews = 234 },
                        new Business { Name = "Turkish Delight", Address = "Saray Mahallesi Atatürk Caddesi No:67", Phone = "+90 242 515 9200", CategoryId = restaurantCategory.Id, Description = "Geleneksel Türk mutfağı ve tatlı çeşitleri", Email = "info@turkishdelight.com", Website = "www.turkishdelight.com", WorkingHours = "10:00-22:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=800", AverageRating = 4.4, TotalReviews = 156 }
                    });
                }

                if (hotelCategory != null)
                {
                    additionalBusinesses.AddRange(new[]
                    {
                        new Business { Name = "Beachfront Hotel", Address = "Kleopatra Sahil Yolu No:45", Phone = "+90 242 528 3000", CategoryId = hotelCategory.Id, Description = "Sahil kenarında lüks konaklama", Email = "info@beachfronthotel.com", Website = "www.beachfronthotel.com", WorkingHours = "7/24", PriceRange = "₺₺₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=800", AverageRating = 4.8, TotalReviews = 445 },
                        new Business { Name = "City Center Hotel", Address = "Merkez Mahallesi No:123", Phone = "+90 242 528 4000", CategoryId = hotelCategory.Id, Description = "Şehir merkezinde konforlu konaklama", Email = "info@citycenterhotel.com", Website = "www.citycenterhotel.com", WorkingHours = "7/24", PriceRange = "₺₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1564501049412-61c2a3083791?w=800", AverageRating = 4.3, TotalReviews = 198 }
                    });
                }

                if (cafeCategory != null)
                {
                    additionalBusinesses.AddRange(new[]
                    {
                        new Business { Name = "Sunset Cafe", Address = "Güller Pınarı Mahallesi No:89", Phone = "+90 242 515 9300", CategoryId = cafeCategory.Id, Description = "Gün batımı manzaralı kafe", Email = "info@sunsetcafe.com", Website = "www.sunsetcafe.com", WorkingHours = "08:00-01:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = true, ImageUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=800", AverageRating = 4.6, TotalReviews = 178 },
                        new Business { Name = "Coffee Corner", Address = "Cikcilli Mahallesi No:34", Phone = "+90 242 515 9400", CategoryId = cafeCategory.Id, Description = "Özel çekim kahveler ve atıştırmalıklar", Email = "info@coffeecorner.com", Website = "www.coffeecorner.com", WorkingHours = "07:00-22:00", PriceRange = "₺₺", IsActive = true, IsApproved = true, IsFeatured = false, ImageUrl = "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?w=800", AverageRating = 4.1, TotalReviews = 92 }
                    });
                }

                await context.Businesses.AddRangeAsync(additionalBusinesses);
                await context.SaveChangesAsync();
            }

            // Haber kontrolü ve ekleme
            if (!await context.News.AnyAsync())
            {
                var news = new List<News>
                {
                    new News 
                    { 
                        Title = "Alanya'da Turizm Sezonu Rekor Kırdı", 
                        Content = "2024 yılı turizm sezonunda Alanya, geçen yıla göre %15 artışla 2.5 milyon turist ağırladı. Özellikle Avrupa'dan gelen turist sayısında önemli artış görülürken, şehirdeki otel doluluk oranları %85'i aştı. Alanya Belediye Başkanı, bu başarının şehrin turizm altyapısındaki yatırımların sonucu olduğunu belirtti.",
                        PublishDate = DateTime.Now.AddDays(-1),
                        AuthorId = null
                    },
                    new News 
                    { 
                        Title = "Kleopatra Plajı'nda Yeni Aktivite Merkezi Açıldı", 
                        Content = "Alanya'nın ünlü Kleopatra Plajı'nda yeni bir su sporları ve aktivite merkezi hizmete girdi. Merkezde dalış, jet ski, parasailing ve su kayağı gibi aktiviteler sunuluyor. Merkez müdürü, güvenlik önlemlerinin en üst seviyede tutulduğunu ve deneyimli eğitmenlerle hizmet verdiklerini açıkladı.",
                        PublishDate = DateTime.Now.AddDays(-3),
                        AuthorId = null
                    },
                    new News 
                    { 
                        Title = "Alanya Kalesi'nde Restorasyon Çalışmaları Tamamlandı", 
                        Content = "UNESCO Dünya Mirası Geçici Listesi'nde yer alan Alanya Kalesi'nde yapılan restorasyon çalışmaları başarıyla tamamlandı. 2 yıl süren çalışmalar sonucunda kale surları ve iç yapılar güçlendirildi. Kale müzesi de yenilendi ve ziyaretçilere daha iyi hizmet verecek şekilde düzenlendi.",
                        PublishDate = DateTime.Now.AddDays(-5),
                        AuthorId = null
                    },
                    new News 
                    { 
                        Title = "Alanya'da Gastronomi Festivali Düzenlendi", 
                        Content = "Alanya Belediyesi tarafından düzenlenen 3. Uluslararası Gastronomi Festivali büyük ilgi gördü. Festivalde yerel lezzetler tanıtılırken, dünyaca ünlü şefler de katılımcılara özel menüler sundu. Festival kapsamında 50'den fazla restoran ve kafe stant açtı.",
                        PublishDate = DateTime.Now.AddDays(-7),
                        AuthorId = null
                    },
                    new News 
                    { 
                        Title = "Alanya Havalimanı'na Yeni Uçuş Seferleri", 
                        Content = "Gazipaşa-Alanya Havalimanı'na yeni uçuş seferleri eklendi. Almanya'nın Frankfurt ve Münih şehirlerinden direkt uçuşlar başladı. Ayrıca İngiltere'nin Manchester şehrinden de charter uçuşlar düzenlenecek. Bu gelişmelerle Alanya'nın turizm potansiyeli daha da artacak.",
                        PublishDate = DateTime.Now.AddDays(-10),
                        AuthorId = null
                    },
                    new News 
                    { 
                        Title = "Alanya'da Sürdürülebilir Turizm Projesi Başlatıldı", 
                        Content = "Alanya Belediyesi ve çevre örgütleri işbirliğiyle sürdürülebilir turizm projesi hayata geçirildi. Proje kapsamında plajlarda çevre dostu uygulamalar başlatılırken, otellerde enerji tasarrufu önlemleri alınıyor. Ayrıca turistlere çevre bilinci konusunda eğitimler verilecek.",
                        PublishDate = DateTime.Now.AddDays(-12),
                        AuthorId = null
                    },
                    new News 
                    { 
                        Title = "Alanya Marina'sında Yeni Tekne Turları", 
                        Content = "Alanya Marina'sında hizmet veren tekne turu şirketleri, yeni rotalar ekledi. Antalya'nın Kaş ve Kalkan bölgelerine günlük turlar düzenlenirken, Akdeniz'in en güzel koylarına da özel turlar organize ediliyor. Teknelerde güvenlik ekipmanları ve deneyimli kaptanlar bulunuyor.",
                        PublishDate = DateTime.Now.AddDays(-15),
                        AuthorId = null
                    },
                    new News 
                    { 
                        Title = "Alanya'da Kültür ve Sanat Merkezi Açıldı", 
                        Content = "Alanya'nın merkezinde yeni bir kültür ve sanat merkezi hizmete girdi. Merkezde sergi salonları, konferans salonu ve sanat atölyeleri bulunuyor. İlk sergi olarak 'Alanya'nın Tarihi' konulu fotoğraf sergisi açıldı. Merkez, yerel sanatçılara da destek veriyor.",
                        PublishDate = DateTime.Now.AddDays(-18),
                        AuthorId = null
                    }
                };
                await context.News.AddRangeAsync(news);
                await context.SaveChangesAsync();
            }
        }
    }
}

