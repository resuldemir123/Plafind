using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class RegisterBusinessViewModel
    {
        // Kullanıcı Bilgileri
        [Required(ErrorMessage = "E-posta gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        [Display(Name = "E-posta Adresi")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre doğrulaması gereklidir.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
        [Display(Name = "Şifre Tekrar")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // İşletme Bilgileri
        [Required(ErrorMessage = "İşletme adı gereklidir.")]
        [Display(Name = "İşletme Adı")]
        public string BusinessName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres gereklidir.")]
        [Display(Name = "Adres")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon gereklidir.")]
        [Display(Name = "Telefon")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Kategori")]
        public int? CategoryId { get; set; }

        [Display(Name = "E-posta")]
        public string? BusinessEmail { get; set; }

        [Display(Name = "Website")]
        public string? Website { get; set; }

        [Display(Name = "Çalışma Saatleri")]
        public string? WorkingHours { get; set; }

        [Display(Name = "Fiyat Aralığı")]
        public string? PriceRange { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Display(Name = "Enlem")]
        public double? Latitude { get; set; }

        [Display(Name = "Boylam")]
        public double? Longitude { get; set; }

        [Required(ErrorMessage = "Kullanım şartlarını kabul etmeniz gerekmektedir.")]
        [Display(Name = "Kullanım Şartları")]
        public bool ConsentAccepted { get; set; } = false;
    }
}

