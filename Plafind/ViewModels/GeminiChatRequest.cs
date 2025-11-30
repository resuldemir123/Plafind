namespace Plafind.ViewModels
{
    public class GeminiChatRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}

