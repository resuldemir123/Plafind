using System.ComponentModel.DataAnnotations;

namespace Plafind.Features.Businesses.ViewModels
{
    public class CreateBusinessViewModel
    {
        [Required(ErrorMessage = "İşletme adı gereklidir")]
        [StringLength(200, ErrorMessage = "İşletme adı en fazla 200 karakter olabilir")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir")]
        public string? Address { get; set; }

        [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir")]
        public string? Phone { get; set; }

        public int? CategoryId { get; set; }

        [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir")]
        public string? Description { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string? Email { get; set; }

        [Url(ErrorMessage = "Geçerli bir web sitesi URL'si giriniz")]
        public string? Website { get; set; }

        public string? WorkingHours { get; set; }
        public string? PriceRange { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ImageUrl { get; set; }
    }
}

