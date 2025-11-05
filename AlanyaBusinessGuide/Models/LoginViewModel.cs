using System.ComponentModel.DataAnnotations;

namespace AlanyaBusinessGuide.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string? Email { get; set; } // Nullable olarak işaretlendi

        [Required(ErrorMessage = "Şifre gereklidir.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; } // Nullable olarak işaretlendi
    }
}