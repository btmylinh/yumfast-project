using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _service;

        public ChatController(IChatService service)
        {
            _service = service;
        }

        [HttpPost]
        public IActionResult Chat([FromBody] ChatRequest req)
        {
            var result = _service.Chat(req.Text ?? "");
            return Ok(result);
        }
    }

    public class ChatRequest
    {
        public string? Text { get; set; }
    }
}
