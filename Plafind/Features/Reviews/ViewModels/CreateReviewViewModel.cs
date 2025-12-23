using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Plafind.Features.Reviews.ViewModels
{
    public class CreateReviewViewModel
    {
        [Required(ErrorMessage = "İşletme seçimi gereklidir")]
        public int BusinessId { get; set; }

        [Required(ErrorMessage = "Yorum metni gereklidir")]
        [StringLength(1000, ErrorMessage = "Yorum en fazla 1000 karakter olabilir")]
        public string? Comment { get; set; }

        [Required(ErrorMessage = "Puan gereklidir")]
        [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır")]
        public int Rating { get; set; }

        public int? BranchId { get; set; }

        public List<IFormFile>? Images { get; set; }
    }
}

