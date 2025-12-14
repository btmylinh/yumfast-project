using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/cart")]
    public class CartController : ControllerBase
    {
        private readonly CartService _service;
        private readonly IInventoryService _inventoryService;

        public CartController(CartService service, IInventoryService inventoryService)
        {
            _service = service;
            _inventoryService = inventoryService;
        }

        // Thêm sản phẩm vào giỏ với inventory validation
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] CartService.AddCartRequest req)
        {
            if (req == null) return BadRequest(new { message = "invalid_request" });

            if (req.Quantity <= 0) req.Quantity = 1;
            if (req.Quantity > 99) req.Quantity = 99;

            // 🆕 Validate inventory
            var hasStock = await _inventoryService.CheckStockAsync(req.ProductId, req.Quantity);
            if (!hasStock)
            {
                return BadRequest(new { 
                    code = "out_of_stock", 
                    message = "Sản phẩm tạm hết hàng hoặc không đủ số lượng" 
                });
            }

            // 🆕 Reserve stock (timeout 15 phút)
            var reserved = await _inventoryService.ReserveStockAsync(req.ProductId, req.Quantity);
            if (!reserved)
            {
                return BadRequest(new { 
                    code = "stock_conflict", 
                    message = "Không thể đặt hàng lúc này, vui lòng thử lại" 
                });
            }

            var result = _service.AddToCart(req, Request, Response);

            return Ok(result);
        }

        // Cập nhật giỏ hàng
        [HttpPost("update")]
        public IActionResult Update([FromBody] List<CartService.CartUpdateRequest> req)
        {
            if (req == null || req.Count == 0)
                return BadRequest(new { message = "empty_request" });

            return Ok(_service.UpdateCart(req, Request, Response));
        }

        // Xóa 1 item
        [HttpDelete("remove")]
        public IActionResult Remove([FromQuery] int productId, [FromQuery] string? optionsKey = null)
        {
            return Ok(_service.RemoveFromCart(productId, optionsKey, Request, Response));
        }

        // Xóa toàn bộ giỏ
        [HttpPost("clear")]
        public IActionResult Clear()
        {
            return Ok(_service.ClearCart(Response));
        }

        // Tổng số lượng
        [HttpGet("count")]
        public IActionResult Count()
        {
            return Ok(new { count = _service.CountItems(Request) });
        }

        // Lấy giỏ hàng chi tiết
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_service.GetCart(Request));
        }
    }
}
