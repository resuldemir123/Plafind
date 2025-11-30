namespace Plafind.Models
{
    public class UserPhoto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ApplicationUser? User { get; set; }
    }
}

