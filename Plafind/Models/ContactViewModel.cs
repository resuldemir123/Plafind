using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Ad Soyad gereklidir.")]
        [Display(Name = "Ad Soyad")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Telefon")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Konu gereklidir.")]
        [Display(Name = "Konu")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mesaj gereklidir.")]
        [Display(Name = "Mesaj")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Mesaj en az 10, en fazla 2000 karakter olmalıdır.")]
        public string Message { get; set; } = string.Empty;
    }
}

