using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AppEL.Services;

namespace AppEL.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly GeminiService _geminiService;

        public ChatbotController(GeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        public IActionResult Index()
        {
            // Thay vì hiển thị trang riêng, chúng ta sẽ chuyển hướng về Home
            return RedirectToAction("Index", "Home");
        }
          [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            if (string.IsNullOrEmpty(request?.Message))
            {
                return BadRequest(new { success = false, message = "Tin nhắn không được để trống" });
            }

            try
            {
                string response;
                
                // Kiểm tra xem có lịch sử trò chuyện không
                if (request.ChatHistory != null && request.ChatHistory.Count > 0)
                {
                    // Gọi phương thức có lịch sử trò chuyện
                    response = await _geminiService.GenerateResponseWithHistoryAsync(request.Message, request.ChatHistory);
                }
                else
                {
                    // Gọi phương thức không có lịch sử
                    response = await _geminiService.GenerateResponseAsync(request.Message);
                }
                
                return Json(new { success = true, message = response });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }}
      public class ChatMessageRequest
    {
        public required string Message { get; set; }
        public List<ChatMessage>? ChatHistory { get; set; }
    }
    
    public class ChatMessage
    {
        public string? role { get; set; }
        public string? content { get; set; }
    }
}
