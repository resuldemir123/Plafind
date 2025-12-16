namespace Plafind.ViewModels.Compare
{
    public class CompareSummaryVM
    {
        public string BestRatingBusinessName { get; set; } = string.Empty;
        public double BestRatingValue { get; set; }
        
        public string BestValueBusinessName { get; set; } = string.Empty;
        public double BestValueScore { get; set; }
        
        public double AverageRating { get; set; }
        
        public Dictionary<string, int> PriceDistribution { get; set; } = new Dictionary<string, int>();
    }
}

