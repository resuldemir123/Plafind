using System.Threading.Tasks;
using Plafind.Services;
using Plafind.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Plafind.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly IGeminiChatService _chatService;

        public AiController(IGeminiChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] GeminiChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { message = "Soru metni gerekli." });
            }

            var response = await _chatService.AskAsync(request.Prompt, request.Latitude, request.Longitude);
            return Ok(new { response });
        }
    }
}

