using System.ComponentModel.DataAnnotations;

namespace AlanyaBusinessGuide.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "E-posta gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string? Email { get; set; } // Nullable olarak işaretlendi

        [Required(ErrorMessage = "Şifre gereklidir.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; } // Nullable olarak işaretlendi

        [Required(ErrorMessage = "Şifre doğrulaması gereklidir.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string? ConfirmPassword { get; set; } // Nullable olarak işaretlendi
    }
}