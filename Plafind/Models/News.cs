using System;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class News
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Başlık zorunludur")]
        [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "İçerik zorunludur")]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; } // Haber resmi URL'si

        public DateTime PublishDate { get; set; }

        public int? ViewCount { get; set; } // Görüntülenme sayısı

        // Yazar bilgisi
        public string? AuthorId { get; set; }
        public ApplicationUser? Author { get; set; }
    }
}