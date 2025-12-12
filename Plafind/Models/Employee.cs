using System;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class Employee
    {
        public int Id { get; set; }
        
        [Required]
        public int BusinessId { get; set; }
        public Business? Business { get; set; }
        
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [StringLength(100)]
        public string? Email { get; set; }
        
        [StringLength(100)]
        public string? Position { get; set; } // Pozisyon: Müdür, Garson, Şef, vb.
        
        [StringLength(50)]
        public string? Department { get; set; } // Departman: Mutfak, Servis, Yönetim, vb.
        
        public DateTime? HireDate { get; set; }
        
        public decimal? Salary { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, OnLeave, Terminated
        
        public bool IsManager { get; set; } = false;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        
        // İşletme sahibi tarafından eklenen notlar
        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
