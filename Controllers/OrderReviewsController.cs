using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Services.Interfaces;
using WebApp.Models;

namespace WebApp.Controllers;

/// <summary>
/// API Controller quản lý đánh giá đơn hàng
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Yêu cầu đăng nhập
public class OrderReviewsController : ControllerBase
{
    private readonly IOrderReviewService _reviewService;
    private readonly ILogger<OrderReviewsController> _logger;

    public OrderReviewsController(
        IOrderReviewService reviewService,
        ILogger<OrderReviewsController> logger)
    {
        _reviewService = reviewService;
        _logger = logger;
    }

    /// <summary>
    /// Tạo đánh giá cho đơn hàng (chỉ được review 1 lần khi đơn hoàn thành)
    /// POST /api/orderreviews
    /// Body: { "orderId": 123, "orderRating": 5, "driverRating": 5, "comment": "...", "images": [...] }
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewViewModel model)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập" });
            }

            // Validate model
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            // Kiểm tra xem có được phép review không
            var canReview = await _reviewService.CanReviewOrderAsync(model.OrderId, userId.Value);
            if (!canReview)
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Không thể đánh giá đơn hàng này (đơn chưa hoàn thành hoặc đã được đánh giá)" 
                });
            }

            // Tạo review (pass userId từ token, không tin model)
            var result = await _reviewService.CreateReviewAsync(model, userId.Value);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("User {UserId} đã đánh giá đơn {OrderId} - Rating: {OrderRating}/{DriverRating}", 
                userId.Value, model.OrderId, model.OrderRating, model.DriverRating);

            return Ok(new { 
                success = true, 
                message = "Đánh giá thành công",
                data = result.Data
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // 403 - Không có quyền review đơn này
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo review cho đơn {OrderId}", model.OrderId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Lấy đánh giá của một đơn hàng cụ thể
    /// GET /api/orderreviews/order/{orderId}
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetOrderReview(long orderId)
    {
        try
        {
            var review = await _reviewService.GetOrderReviewAsync(orderId);
            
            if (review == null)
            {
                return NotFound(new { success = false, message = "Chưa có đánh giá cho đơn hàng này" });
            }

            return Ok(new { success = true, data = review });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy review của đơn {OrderId}", orderId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Lấy danh sách đánh giá của một tài xế (phân trang)
    /// GET /api/orderreviews/driver/{driverId}?page=1 (pageSize mặc định 10)
    /// </summary>
    [HttpGet("driver/{driverId}")]
    [AllowAnonymous] // Cho phép xem công khai để user chọn driver
    public async Task<IActionResult> GetDriverReviews(long driverId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            // Validate pagination
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var reviews = await _reviewService.GetDriverReviewsAsync(driverId, page, pageSize);

            return Ok(new { 
                success = true, 
                data = reviews,
                pagination = new {
                    page,
                    pageSize,
                    hasMore = reviews.Count == pageSize // Nếu đủ pageSize thì có thể còn trang sau
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy reviews của driver {DriverId}", driverId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Admin trả lời đánh giá
    /// POST /api/orderreviews/{orderId}/reply
    /// Body: { "reply": "Cảm ơn bạn đã đánh giá..." }
    /// </summary>
    [HttpPost("{orderId}/reply")]
    [Authorize(Roles = "admin")] // Chỉ admin
    public async Task<IActionResult> AdminReply(long orderId, [FromBody] AdminReplyRequest request)
    {
        try
        {
            var adminId = GetUserId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Không xác định được admin" });
            }

            // Validate reply
            if (string.IsNullOrWhiteSpace(request.Reply))
            {
                return BadRequest(new { success = false, message = "Nội dung trả lời không được để trống" });
            }

            if (request.Reply.Length > 1000)
            {
                return BadRequest(new { success = false, message = "Nội dung trả lời không được quá 1000 ký tự" });
            }

            // Kiểm tra review có tồn tại không
            var review = await _reviewService.GetOrderReviewAsync(orderId);
            if (review == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đánh giá" });
            }

            // Trả lời review
            var result = await _reviewService.AdminReplyAsync(orderId, request.Reply, adminId.Value);
            
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("Admin {AdminId} đã trả lời review của đơn {OrderId}", 
                adminId.Value, orderId);

            return Ok(new { success = true, message = "Trả lời đánh giá thành công" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi admin trả lời review của đơn {OrderId}", orderId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Lấy thống kê rating trung bình của tài xế
    /// GET /api/orderreviews/driver/{driverId}/stats
    /// </summary>
    [HttpGet("driver/{driverId}/stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDriverReviewStats(long driverId)
    {
        try
        {
            // Lấy tất cả reviews của driver để tính stats
            var allReviews = await _reviewService.GetDriverReviewsAsync(driverId, 1, 1000);
            
            if (!allReviews.Any())
            {
                return Ok(new { 
                    success = true, 
                    data = new {
                        driverId,
                        totalReviews = 0,
                        averageRating = 0.0,
                        ratingDistribution = new Dictionary<int, int>()
                    }
                });
            }

            // Tính toán stats
            var totalReviews = allReviews.Count;
            var averageRating = allReviews
                .Where(r => r.DriverRating.HasValue)
                .Average(r => r.DriverRating!.Value);
            
            // Phân bố rating (1-5 sao có bao nhiêu reviews)
            var ratingDistribution = allReviews
                .Where(r => r.DriverRating.HasValue)
                .GroupBy(r => r.DriverRating!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            return Ok(new { 
                success = true, 
                data = new {
                    driverId,
                    totalReviews,
                    averageRating = Math.Round((decimal)averageRating, 2),
                    ratingDistribution
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy stats review của driver {DriverId}", driverId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    // ==================== PRIVATE HELPERS ====================

    /// <summary>
    /// Lấy User ID từ JWT claims
    /// </summary>
    private long? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }
}

/// <summary>
/// Request model để admin trả lời review
/// </summary>
public class AdminReplyRequest
{
    public string Reply { get; set; } = string.Empty;
}
