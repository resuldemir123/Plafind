namespace Plafind.Services
{
    public interface ISentimentAnalysisService
    {
        Task<SentimentAnalysisResult> AnalyzeBusinessReviewsAsync(int businessId, int maxReviews = 50);
    }

    public class SentimentAnalysisResult
    {
        public int BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new List<string>();
        public List<string> Weaknesses { get; set; } = new List<string>();
        public List<string> ImprovementAreas { get; set; } = new List<string>();
        public double OverallSatisfactionScore { get; set; } // 0-10 arası
        public Dictionary<string, double> CategoryScores { get; set; } = new Dictionary<string, double>();
        public int TotalReviewsAnalyzed { get; set; }
        public DateTime AnalysisDate { get; set; } = DateTime.Now;
    }
}

