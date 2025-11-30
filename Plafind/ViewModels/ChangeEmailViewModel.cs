using System.ComponentModel.DataAnnotations;

namespace Plafind.ViewModels
{
    public class ChangeEmailViewModel
    {
        [Required(ErrorMessage = "Yeni e-posta adresi gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        [Display(Name = "Yeni E-posta")]
        public string? NewEmail { get; set; }

        [Required(ErrorMessage = "Mevcut şifrenizi girmeniz gerekiyor.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mevcut Şifre")]
        public string? CurrentPassword { get; set; }
    }
}

