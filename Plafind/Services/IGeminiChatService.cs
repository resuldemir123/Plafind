using System.Threading.Tasks;

namespace Plafind.Services
{
    public interface IGeminiChatService
    {
        Task<string> AskAsync(string prompt, double? latitude = null, double? longitude = null);
    }
}

