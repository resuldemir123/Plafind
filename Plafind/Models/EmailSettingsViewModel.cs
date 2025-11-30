using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class EmailSettingsViewModel
    {
        [Required(ErrorMessage = "SMTP sunucu gereklidir.")]
        [Display(Name = "SMTP Sunucu")]
        public string SmtpServer { get; set; } = string.Empty;

        [Required(ErrorMessage = "SMTP Port gereklidir.")]
        [Display(Name = "SMTP Port")]
        public int SmtpPort { get; set; }

        [Required(ErrorMessage = "E-posta adresi gereklidir.")]
        [EmailAddress]
        [Display(Name = "E-posta Adresi")]
        public string EmailAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir.")]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "SSL Kullan")]
        public bool UseSsl { get; set; } = true;
    }
}

