using System.Threading.Tasks;

namespace AlanyaBusinessGuide.Services
{
    public interface IGeminiChatService
    {
        Task<string> AskAsync(string prompt, double? latitude = null, double? longitude = null);
    }
}

