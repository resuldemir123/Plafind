using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Plafind.Models;

namespace Plafind.ViewModels
{
    public class UserProfileViewModel
    {
        [Display(Name = "Kullanıcı Adı")]
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-32 karakter arasında olmalıdır.")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Kullanıcı adı sadece harf, rakam ve . _ - karakterlerini içerebilir.")]
        public string? UserName { get; set; }

        [Display(Name = "E-posta")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string? Email { get; set; }

        [Display(Name = "Görünen Ad")]
        [StringLength(48, ErrorMessage = "Görünen ad en fazla 48 karakter olabilir.")]
        public string? DisplayName { get; set; }

        [Display(Name = "Ad Soyad")]
        [StringLength(64, ErrorMessage = "Ad soyad en fazla 64 karakter olabilir.")]
        public string? FullName { get; set; }

        [Phone]
        [Display(Name = "Telefon")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Şehir")]
        [StringLength(100, ErrorMessage = "Şehir adı en fazla 100 karakter olabilir.")]
        public string? City { get; set; }

        [Display(Name = "Ülke")]
        [StringLength(100, ErrorMessage = "Ülke adı en fazla 100 karakter olabilir.")]
        public string? Country { get; set; }

        [Display(Name = "Web Sitesi")]
        [Url(ErrorMessage = "Geçerli bir URL girin.")]
        public string? Website { get; set; }

        [Display(Name = "Hakkımda")]
        [StringLength(500, ErrorMessage = "Hakkımda metni en fazla 500 karakter olabilir.")]
        public string? Bio { get; set; }

        public string? CurrentAvatarUrl { get; set; }

        [Display(Name = "Avatar Seç")]
        public string? SelectedAvatar { get; set; }

        [Display(Name = "Avatar Yükle")]
        public IFormFile? AvatarFile { get; set; }

        public IEnumerable<string> DefaultAvatars { get; set; } = Enumerable.Empty<string>();

        public DateTime CreatedDate { get; set; }
        public int FavoritesCount { get; set; }
        public int ReviewsCount { get; set; }

        [Display(Name = "Fotoğraf Yükle")]
        public IFormFile[]? PhotoFiles { get; set; }

        public List<UserPhoto>? Photos { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

