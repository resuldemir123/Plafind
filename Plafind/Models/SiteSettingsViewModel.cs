using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class SiteSettingsViewModel
    {
        [Display(Name = "Site Başlığı")]
        public string? SiteTitle { get; set; }

        [Display(Name = "Site Açıklaması")]
        public string? SiteDescription { get; set; }

        [Display(Name = "İletişim E-postası")]
        [EmailAddress]
        public string? ContactEmail { get; set; }

        [Display(Name = "İletişim Telefonu")]
        public string? ContactPhone { get; set; }

        [Display(Name = "Bakım Modu")]
        public bool MaintenanceMode { get; set; }
    }
}

