using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotifyProject.Models;
using SpotifyProject.Services;
using System.Threading.Tasks;

namespace SpotifyProject.Controllers
{
    // [Authorize]
    public class ChatController : Controller
    {
        private readonly ChatbotService _chatbotService;

        public ChatController(ChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        // GET: Chat/Index (changed from Chatbot/Index)
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Add this for security
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return BadRequest(new { message = "Message cannot be empty" });
            }

            try
            {
                var response = await _chatbotService.ProcessMessage(request.Message);
                return Ok(new { message = response });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }
    }
}