namespace Plafind.ViewModels.Compare
{
    public class CompareIndexVM
    {
        public List<CompareBusinessVM> Items { get; set; } = new List<CompareBusinessVM>();
        public CompareSummaryVM Summary { get; set; } = new CompareSummaryVM();
        public CategoryCompatibilityVM CategoryInfo { get; set; } = new CategoryCompatibilityVM();
        
        // Grafik dataları
        public List<string> Labels { get; set; } = new List<string>();
        public List<double> Ratings { get; set; } = new List<double>();
        public List<int> PriceValues { get; set; } = new List<int>();
        public List<double> ValueScores { get; set; } = new List<double>();
        
        // Mesaj
        public string? EmptyMessage { get; set; }
    }
}

