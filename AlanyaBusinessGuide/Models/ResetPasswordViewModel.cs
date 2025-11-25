using System.ComponentModel.DataAnnotations;

namespace AlanyaBusinessGuide.Models
{
    public class ResetPasswordViewModel
    {
        [Display(Name = "Telefon Numarası")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası girin.")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Doğrulama Kodu")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Doğrulama kodu 6 haneli olmalıdır.")]
        public string? Code { get; set; }

        public string? UserId { get; set; }
        public string? Token { get; set; }

        [Required(ErrorMessage = "Yeni şifre gereklidir.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre doğrulaması gereklidir.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifreyi Tekrar Girin")]
        [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

