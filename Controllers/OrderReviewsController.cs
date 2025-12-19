using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Services.Interfaces;
using WebApp.Models;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderReviewsController : ControllerBase
{
    private readonly IOrderReviewService _reviewService;
    private readonly ILogger<OrderReviewsController> _logger;

    public OrderReviewsController(IOrderReviewService reviewService, ILogger<OrderReviewsController> logger)
    {
        _reviewService = reviewService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewViewModel model)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập" });

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            var canReview = await _reviewService.CanReviewOrderAsync(model.OrderId, userId.Value);
            if (!canReview)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Không thể đánh giá đơn hàng này (đơn chưa hoàn thành hoặc đã được đánh giá)"
                });
            }

            var result = await _reviewService.CreateReviewAsync(model, userId.Value);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            _logger.LogInformation("User {UserId} reviewed order {OrderId} - {OrderRating}/{DriverRating}",
                userId.Value, model.OrderId, model.OrderRating, model.DriverRating);

            return Ok(new { success = true, message = "Đánh giá thành công", data = result.Data });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review for order {OrderId}", model.OrderId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetOrderReview(long orderId)
    {
        try
        {
            var review = await _reviewService.GetOrderReviewAsync(orderId);
            if (review == null)
                return NotFound(new { success = false, message = "Chưa có đánh giá cho đơn hàng này" });

            return Ok(new { success = true, data = review });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order review {OrderId}", orderId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    [HttpGet("order/{orderId}/details")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderReviewDetails(long orderId)
    {
        try
        {
            var details = await _reviewService.GetOrderReviewDetailsAsync(orderId);
            if (details == null)
                return NotFound(new { success = false, message = "Chưa có đánh giá cho đơn hàng này" });

            return Ok(new { success = true, data = details });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order review details {OrderId}", orderId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    [HttpGet("driver/{driverId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDriverReviews(long driverId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var reviews = await _reviewService.GetDriverReviewsAsync(driverId, page, pageSize);

            return Ok(new
            {
                success = true,
                data = reviews,
                pagination = new
                {
                    page,
                    pageSize,
                    hasMore = reviews.Count == pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver reviews {DriverId}", driverId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    [HttpPost("{orderId}/reply")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> AdminReply(long orderId, [FromBody] AdminReplyRequest request)
    {
        try
        {
            var adminId = GetUserId();
            if (adminId == null)
                return Unauthorized(new { success = false, message = "Không xác định được admin" });

            if (string.IsNullOrWhiteSpace(request.Reply))
                return BadRequest(new { success = false, message = "Nội dung trả lời không được để trống" });

            if (request.Reply.Length > 1000)
                return BadRequest(new { success = false, message = "Nội dung trả lời không được quá 1000 ký tự" });

            var review = await _reviewService.GetOrderReviewAsync(orderId);
            if (review == null)
                return NotFound(new { success = false, message = "Không tìm thấy đánh giá" });

            // ✅ FIX: reply theo ORDER_ID, không nhầm sang review.id
            var result = await _reviewService.AdminReplyByOrderIdAsync(orderId, request.Reply, adminId.Value);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = "Trả lời đánh giá thành công" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error admin reply order {OrderId}", orderId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    [HttpGet("driver/{driverId}/stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDriverReviewStats(long driverId)
    {
        try
        {
            var allReviews = await _reviewService.GetDriverReviewsAsync(driverId, 1, 1000);

            var rated = allReviews.Where(r => r.DriverRating.HasValue).ToList();
            if (!rated.Any())
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        driverId,
                        totalReviews = allReviews.Count,
                        averageRating = 0.0m,
                        ratingDistribution = new Dictionary<int, int>()
                    }
                });
            }

            var avg = rated.Average(r => r.DriverRating!.Value);
            var distribution = rated.GroupBy(r => r.DriverRating!.Value).ToDictionary(g => g.Key, g => g.Count());

            return Ok(new
            {
                success = true,
                data = new
                {
                    driverId,
                    totalReviews = allReviews.Count,
                    averageRating = Math.Round((decimal)avg, 2),
                    ratingDistribution = distribution
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error driver stats {DriverId}", driverId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    private long? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return null;
        return userId;
    }
}

public class AdminReplyRequest
{
    public string Reply { get; set; } = string.Empty;
}
