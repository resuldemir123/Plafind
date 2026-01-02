using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class Collection
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [StringLength(500)]
        public string? ImageUrl { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        
        public string? CreatedBy { get; set; } // Admin kullanıcı ID
        
        // Navigation properties
        public ICollection<CollectionBusiness> CollectionBusinesses { get; set; } = new List<CollectionBusiness>();
    }
    
    public class CollectionBusiness
    {
        public int Id { get; set; }
        public int CollectionId { get; set; }
        public int BusinessId { get; set; }
        public int DisplayOrder { get; set; } = 0; // Koleksiyon içindeki sıralama
        
        // Navigation properties
        public Collection? Collection { get; set; }
        public Business? Business { get; set; }
    }
}


