using System.Collections.Generic;

namespace AlanyaBusinessGuide.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<Business>? Businesses { get; set; }
    }
}