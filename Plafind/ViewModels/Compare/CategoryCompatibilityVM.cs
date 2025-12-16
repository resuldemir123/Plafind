namespace Plafind.ViewModels.Compare
{
    public class CategoryCompatibilityVM
    {
        public bool AllSameCategory { get; set; }
        public string? CommonCategoryName { get; set; }
        public List<string> AllCategories { get; set; } = new List<string>();
    }
}

