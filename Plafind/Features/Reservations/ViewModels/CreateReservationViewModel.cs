using System.ComponentModel.DataAnnotations;

namespace Plafind.Features.Reservations.ViewModels
{
    public class CreateReservationViewModel
    {
        [Required(ErrorMessage = "İşletme seçimi gereklidir")]
        public int BusinessId { get; set; }

        [Required(ErrorMessage = "Rezervasyon tarihi gereklidir")]
        [DataType(DataType.Date)]
        public DateTime RequestedDate { get; set; }

        [Required(ErrorMessage = "Rezervasyon saati gereklidir")]
        [DataType(DataType.Time)]
        public TimeSpan RequestedTime { get; set; }

        [Required(ErrorMessage = "Kişi sayısı gereklidir")]
        [Range(1, 50, ErrorMessage = "Kişi sayısı 1 ile 50 arasında olmalıdır")]
        public int NumberOfPeople { get; set; }

        [StringLength(500, ErrorMessage = "Notlar en fazla 500 karakter olabilir")]
        public string? Notes { get; set; }

        [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir")]
        public string? ContactPhone { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string? ContactEmail { get; set; }

        public int? BranchId { get; set; }
    }
}

