using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Models;

namespace Plafind.Scripts
{
    /// <summary>
    /// Gerçek işletme verilerini eklemek için script
    /// Kullanım: Program.cs'de veya bir controller'da çağırın
    /// </summary>
    public static class SeedRealBusinesses
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // 1. Mevcut tüm işletmeleri sil
            var existingBusinesses = await context.Businesses.ToListAsync();
            if (existingBusinesses.Any())
            {
                context.Businesses.RemoveRange(existingBusinesses);
                await context.SaveChangesAsync();
                Console.WriteLine($"{existingBusinesses.Count} işletme silindi.");
            }

            // 2. Kategorileri kontrol et ve gerekirse oluştur
            var categoryMap = await EnsureCategoriesAsync(context);

            // 3. Yeni işletmeleri ekle
            var businesses = new List<Business>
            {
                new Business
                {
                    Name = "Le Chevy Restaurant",
                    CategoryId = categoryMap["restaurant"],
                    Address = "Saray Mah., Müze Sok. No:2A, 07400 Alanya/Antalya, Türkiye",
                    Phone = "+90 533 501 62 47",
                    Email = null,
                    Website = "https://www.instagram.com/lechevy/",
                    WorkingHours = null,
                    Description = "Alanya merkezde yer alan, et ve dünya mutfağı ağırlıklı, vintage-modern karışımı bir restoran ve steakhouse.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = true,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Ravza Restaurant",
                    CategoryId = categoryMap["restaurant"],
                    Address = "Çarşı Mah. 10. Sk. No:16, 07400 Alanya/Antalya, Türkiye",
                    Phone = "+90 242 513 39 83",
                    Email = "info@ravzarestaurant.com",
                    Website = "https://www.ravzarestaurant.com/",
                    WorkingHours = "Her gün 07:00–00:00",
                    Description = "1955'ten beri hizmet veren, geleneksel Türk yemekleri ve Alanya yöresel mutfağı sunan köklü aile restoranı.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = true,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Mezze Grill Restaurant & Ocakbaşı",
                    CategoryId = categoryMap["restaurant"],
                    Address = "Saray Mah., Atatürk Cad. No:84/B-C, 07400 Alanya/Antalya, Türkiye",
                    Phone = "+90 242 515 40 87",
                    Email = null,
                    Website = null,
                    WorkingHours = null,
                    Description = "Steak, kebap ve deniz ürünleri ağırlıklı; canlı müzikli, hem yerel hem uluslararası lezzetler sunan popüler ocakbaşı restoranı.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = false,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Hasır Restaurant & Bar",
                    CategoryId = categoryMap["restaurant"],
                    Address = "Saray Mah., Güzelyalı Cd. No:14, 07400 Alanya/Antalya, Türkiye",
                    Phone = "+90 532 592 72 15",
                    Email = null,
                    Website = "https://www.hasirrestaurantalanya.com/",
                    WorkingHours = "Her gün 08:30–02:00",
                    Description = "Kleopatra bölgesinde, Türk ve Akdeniz mutfağı, steak, deniz ürünleri ve bar konseptini birleştiren, canlı ve turistik bir mekân.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = true,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Alanya Olivia Gourmet Restaurant & Cafe Bar",
                    CategoryId = categoryMap["restaurant"],
                    Address = "Güllerpınarı Mah., Ahmet Tokuş Blv. No:35/D, 07400 Alanya/Antalya, Türkiye",
                    Phone = "+90 242 519 09 07",
                    Email = null,
                    Website = "https://www.facebook.com/oliviagourmetalanya",
                    WorkingHours = "Her gün 09:30–23:30",
                    Description = "Steak ve Akdeniz mutfağı odaklı, servis kalitesi ve atmosferiyle öne çıkan bir restoran ve cafe bar.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = false,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Soul of Kitchen Restaurant",
                    CategoryId = categoryMap["restaurant"],
                    Address = "Güller Pınarı Mah., Ahmet Tokuş Blv. No:44, 07400 Alanya/Antalya, Türkiye",
                    Phone = "+90 549 565 17 34",
                    Email = "reservation@soulofkitchen.com",
                    Website = "https://www.soulofkitchen.com/",
                    WorkingHours = "Her gün yaklaşık 08:00–02:15",
                    Description = "Deniz kenarında, özellikle Instagram'lık sunumları ve akşam atmosferiyle bilinen, Türk ve uluslararası mutfak karışımı bir restoran.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = true,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Kale Panorama Restaurant",
                    CategoryId = categoryMap["restaurant"],
                    Address = "Tophane Mah., Kale Cd. No:15, Alanya/Antalya, Türkiye",
                    Phone = "+90 242 512 62 82",
                    Email = "info@alanyapanorama.com",
                    Website = "https://alanyapanorama.com/",
                    WorkingHours = null,
                    Description = "Alanya kalesi bölgesinde, şehir ve deniz manzaralı, Türk ve dünya mutfağı sunan panoramik manzaralı restoran.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = true,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Kleopatra Blue Hawaii Restaurant",
                    CategoryId = categoryMap["restaurant"],
                    Address = "Saray Mah., Mehmet Şükrü Ulusoy Cd. No:3A, 07400 Alanya/Antalya, Türkiye",
                    Phone = "+90 538 334 10 27",
                    Email = null,
                    Website = null,
                    WorkingHours = "Her gün 09:00–00:00",
                    Description = "Kleopatra Plajı yakınında, kokteylleri, karışık ızgara ve deniz ürünleriyle öne çıkan restoran.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = false,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Sunprime C-Lounge Hotel",
                    CategoryId = categoryMap["hotel"],
                    Address = "Tosmur Mah., Ahmet Tokuş Cad. 7. Sok. No:1, Oba Mevkii, 07400 Alanya/Antalya, Türkiye",
                    Phone = "+90 242 514 20 00",
                    Email = "info@c-loungehotel.com",
                    Website = "https://www.c-loungehotel.com/",
                    WorkingHours = "24 saat açık (otel)",
                    Description = "Sadece yetişkinlere hizmet veren, deniz kenarında, spa ve her şey dahil konsept sunan 5 yıldızlı otel.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = true,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Sirius Deluxe Hotel",
                    CategoryId = categoryMap["hotel"],
                    Address = "Fuğla Mah., Marina Sok. No:7, 07407 Türkler / Alanya, Antalya, Türkiye",
                    Phone = "+90 242 510 52 00",
                    Email = "info@siriusdeluxe.com",
                    Website = "https://www.siriusdeluxe.com/",
                    WorkingHours = "24 saat açık (otel)",
                    Description = "Türkler bölgesinde, denize sıfır, aile dostu ve spa imkanları olan her şey dahil konseptli lüks otel.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = true,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Cleopatra Blue Hawaii Hotel",
                    CategoryId = categoryMap["hotel"],
                    Address = "Saray Mah., Atatürk Caddesi 148, 07400 Alanya/Antalya, Türkiye",
                    Phone = "+90 242 512 69 69",
                    Email = "info@bluehawaihotel.com",
                    Website = "https://bluehawaihotel.com/",
                    WorkingHours = "24 saat açık (otel)",
                    Description = "Kleopatra Plajı üzerinde yer alan, sahil konumlu, restoran ve spa hizmetleri sunan otel.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = true,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Alanyum Alışveriş ve Eğlence Merkezi",
                    CategoryId = categoryMap["shopping_mall"],
                    Address = "Cumhuriyet Mah., Keykubat Blv. No:219, 07400 Alanya/Antalya, Türkiye",
                    Phone = "+90 242 515 11 34",
                    Email = "info@alanyum.com.tr",
                    Website = "https://www.alanyum.com/",
                    WorkingHours = "Her gün 09:30–22:00",
                    Description = "Alanya'nın en bilinen AVM'lerinden biri; ulusal ve uluslararası markalar, market, sinema ve yeme-içme alanları bulunuyor.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = true,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Mall of Alanya",
                    CategoryId = categoryMap["shopping_mall"],
                    Address = "Konaklı Mah., Mustafa Kemal Bulvarı No:81, 07475 Alanya/Antalya, Türkiye",
                    Phone = "+90 535 975 92 95",
                    Email = null,
                    Website = null,
                    WorkingHours = "Her gün 10:00–22:00",
                    Description = "Konaklı bölgesinde konumlu, çeşitli giyim, elektronik ve yeme-içme markalarını barındıran kompakt alışveriş merkezi.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = false,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Yekta Mall Alışveriş ve Eğlence Merkezi",
                    CategoryId = categoryMap["shopping_mall"],
                    Address = "Mahmutlar Mah., Atatürk Cd. No:123, 07450 Alanya/Antalya, Türkiye",
                    Phone = "+90 537 745 00 00",
                    Email = "info@yektamall.com",
                    Website = "https://yektamall.com/",
                    WorkingHours = "Her gün 10:00–22:00",
                    Description = "Mahmutlar'da yer alan yeni nesil AVM; giyim markaları, kahve zincirleri ve restoranlarıyla bölgesel bir çekim merkezi.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = true,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Neva Outlet Alanya (Keykubat)",
                    CategoryId = categoryMap["outlet_store"],
                    Address = "Keykubat Blv. No:274A, 07400 Alanya/Antalya, Türkiye",
                    Phone = "+90 850 888 70 07",
                    Email = "hi@nevasonline.com",
                    Website = "https://nevaoutlet.com/",
                    WorkingHours = "Her gün 09:00–23:00 (genel zincir saatleri)",
                    Description = "Çeşitli giyim ve markalı ürünlerin indirimli satıldığı Neva Outlet zincirinin Alanya sahil yolu üzerindeki şubesi.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = false,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Neva Outlet Okurcalar Soho",
                    CategoryId = categoryMap["outlet_store"],
                    Address = "Cumhuriyet Blv. No:274/A, Okurcalar Mah., 07415 Alanya/Antalya, Türkiye",
                    Phone = "+90 850 888 70 07",
                    Email = "hi@nevasonline.com",
                    Website = "https://nevaoutlet.com/",
                    WorkingHours = "Her gün 09:00–23:00",
                    Description = "Okurcalar bölgesinde konumlu, global markaları indirimli fiyatlarla sunan büyük outlet merkezi.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = false,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Alanya Marina",
                    CategoryId = categoryMap["marina"],
                    Address = "Akhan Mevkii, Emirgan Bulvarı, Alanya Marina Yat Limanı, 07400 Alanya/Antalya, Türkiye",
                    Phone = "+90 242 511 34 00",
                    Email = "info@alanyamarina.com.tr",
                    Website = "https://www.alanyamarina.com.tr/",
                    WorkingHours = "Genel olarak 24 saat hizmet veren marina",
                    Description = "Tekne ve yatlar için bağlama alanları, restoran, market, spor salonu, teknik servis ve sosyal alanlar barındıran modern marina tesisi.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = true,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Crazy Horse / Crazy Sushi",
                    CategoryId = categoryMap["restaurant"],
                    Address = "Şekerhane Mah., Hükümet Cd., 07400 Alanya/Antalya, Türkiye",
                    Phone = "+90 242 511 00 21",
                    Email = null,
                    Website = null,
                    WorkingHours = "Bar yaklaşık 03:00'a kadar açık",
                    Description = "Alanya liman bölgesinde, Meksika mutfağı ve sushi sunan; barı geç saatlere kadar açık olan popüler mekan.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = false,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "LC Waikiki Alanya Atatürk",
                    CategoryId = categoryMap["clothing_store"],
                    Address = "Şekerhane Mah., Yaylayolu Cad. No:4/A, 07400 Alanya/Antalya, Türkiye",
                    Phone = null,
                    Email = null,
                    Website = "https://www.lcwaikiki.com/tr-TR/TR",
                    WorkingHours = "Her gün 10:00–22:00",
                    Description = "Türkiye'nin önde gelen giyim zincirlerinden LC Waikiki'nin Alanya merkezdeki Atatürk şubesi.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = false,
                    CreatedDate = DateTime.Now
                },
                new Business
                {
                    Name = "Boyner Alanya (Alanyum AVM Şubesi)",
                    CategoryId = categoryMap["department_store"],
                    Address = "Cumhuriyet Mah., Keykubat Blv. No:219, Alanyum AVM, 07400 Alanya/Antalya, Türkiye",
                    Phone = null,
                    Email = null,
                    Website = "https://www.boyner.com.tr/",
                    WorkingHours = "AVM çalışma saatlerine bağlı (genelde 09:30–22:00)",
                    Description = "Alanyum AVM içinde yer alan, giyim, kozmetik ve ev ürünleri satan büyük çok katlı mağaza.",
                    IsActive = true,
                    IsApproved = true,
                    IsFeatured = false,
                    CreatedDate = DateTime.Now
                }
            };

            await context.Businesses.AddRangeAsync(businesses);
            await context.SaveChangesAsync();
            Console.WriteLine($"{businesses.Count} yeni işletme eklendi.");
        }

        private static async Task<Dictionary<string, int>> EnsureCategoriesAsync(ApplicationDbContext context)
        {
            var categoryMap = new Dictionary<string, int>();

            // Kategori eşleştirmeleri
            var categoryMappings = new Dictionary<string, string>
            {
                { "restaurant", "Restoran" },
                { "hotel", "Otel" },
                { "shopping_mall", "Alışveriş Merkezi" },
                { "outlet_store", "Outlet" },
                { "marina", "Marina" },
                { "restaurant_bar", "Restoran" },
                { "clothing_store", "Mağaza" },
                { "department_store", "Mağaza" }
            };

            foreach (var mapping in categoryMappings)
            {
                var categoryName = mapping.Value;
                var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);

                if (category == null)
                {
                    category = new Category
                    {
                        Name = categoryName,
                        Description = GetCategoryDescription(categoryName)
                    };
                    await context.Categories.AddAsync(category);
                    await context.SaveChangesAsync();
                }

                categoryMap[mapping.Key] = category.Id;
            }

            return categoryMap;
        }

        private static string GetCategoryDescription(string categoryName)
        {
            return categoryName switch
            {
                "Restoran" => "Yemek ve içecek hizmetleri",
                "Otel" => "Konaklama hizmetleri",
                "Alışveriş Merkezi" => "Alışveriş merkezleri ve AVM'ler",
                "Outlet" => "Outlet mağazaları ve indirimli alışveriş",
                "Marina" => "Yat limanı ve marina hizmetleri",
                "Mağaza" => "Giyim ve genel mağazalar",
                _ => $"{categoryName} kategorisi"
            };
        }
    }
}
