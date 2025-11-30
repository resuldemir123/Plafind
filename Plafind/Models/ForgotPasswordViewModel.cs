using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Telefon numarası gereklidir.")]
        [Display(Name = "Telefon Numarası")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası girin.")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}

