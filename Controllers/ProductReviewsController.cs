using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Models;
using WebApp.Services.Interfaces;

namespace WebApp.Controllers;

/// <summary>
/// API Controller quản lý đánh giá sản phẩm
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Yêu cầu đăng nhập (trừ một số endpoints public)
public class ProductReviewsController : ControllerBase
{
    private readonly IProductReviewService _reviewService;
    private readonly ILogger<ProductReviewsController> _logger;

    public ProductReviewsController(
        IProductReviewService reviewService,
        ILogger<ProductReviewsController> logger)
    {
        _reviewService = reviewService;
        _logger = logger;
    }

    /// <summary>
    /// Tạo đánh giá cho sản phẩm (chỉ user đã mua mới được review)
    /// POST /api/productreviews
    /// Body: { "productId": 123, "rating": 5, "comment": "..." }
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateProductReviewViewModel model)
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

            // Validate rating
            if (model.Rating < 1 || model.Rating > 5)
            {
                return BadRequest(new { success = false, message = "Rating phải từ 1-5 sao" });
            }

            // Kiểm tra xem có được phép review không
            var canReview = await _reviewService.CanReviewProductAsync(model.ProductId, userId.Value);
            if (!canReview)
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Không thể đánh giá sản phẩm này (chưa mua hoặc đã đánh giá)" 
                });
            }

            // Tạo review
            var result = await _reviewService.CreateReviewAsync(model, userId.Value);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("User {UserId} đã đánh giá sản phẩm {ProductId} - Rating: {Rating}", 
                userId.Value, model.ProductId, model.Rating);

            return Ok(new { 
                success = true, 
                message = result.Message,
                data = result.Data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo review cho sản phẩm {ProductId}", model.ProductId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Lấy danh sách reviews của sản phẩm (chỉ reviews đã approved, public)
    /// GET /api/productreviews/product/{productId}?page=1&amp;pageSize=10
    /// </summary>
    [HttpGet("product/{productId}")]
    [AllowAnonymous] // Cho phép xem công khai
    public async Task<IActionResult> GetProductReviews(long productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            // Validate pagination
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var reviews = await _reviewService.GetProductReviewsAsync(productId, page, pageSize);

            return Ok(new { 
                success = true, 
                data = reviews,
                pagination = new {
                    page,
                    pageSize,
                    hasMore = reviews.Count == pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy reviews của sản phẩm {ProductId}", productId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Lấy thống kê reviews của sản phẩm (public)
    /// GET /api/productreviews/product/{productId}/stats
    /// </summary>
    [HttpGet("product/{productId}/stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductReviewStats(long productId)
    {
        try
        {
            var stats = await _reviewService.GetProductReviewStatsAsync(productId);

            return Ok(new { 
                success = true, 
                data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy stats review của sản phẩm {ProductId}", productId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Lấy reviews của user (để user xem lại reviews của mình)
    /// GET /api/productreviews/user/{userId}
    /// </summary>
    [HttpGet("user/{userId}")]
    public Task<IActionResult> GetUserReviews(long userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var currentUserId = GetUserId();
            if (currentUserId == null)
            {
                return Task.FromResult<IActionResult>(Unauthorized(new { success = false, message = "Vui lòng đăng nhập" }));
            }

            // Chỉ cho phép xem reviews của chính mình
            if (currentUserId.Value != userId)
            {
                return Task.FromResult<IActionResult>(Forbid());
            }

            // Validate pagination
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            // TODO: Implement GetUserReviewsAsync trong service nếu cần
            // Hiện tại có thể dùng GetProductReviewsAsync và filter theo user_id
            // Hoặc tạo method mới trong service

            return Task.FromResult<IActionResult>(Ok(new { 
                success = true, 
                message = "Chức năng đang được phát triển",
                data = new List<ProductReview>()
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy reviews của user {UserId}", userId);
            return Task.FromResult<IActionResult>(StatusCode(500, new { success = false, message = "Lỗi server" }));
        }
    }

    /// <summary>
    /// Admin approve/reject review
    /// PUT /api/productreviews/{reviewId}/status
    /// Body: { "status": 1 } // 0=pending, 1=approved
    /// </summary>
    [HttpPut("{reviewId}/status")]
    [Authorize(Roles = "admin")] // Chỉ admin
    public async Task<IActionResult> UpdateReviewStatus(long reviewId, [FromBody] UpdateReviewStatusRequest request)
    {
        try
        {
            var adminId = GetUserId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Không xác định được admin" });
            }

            // Validate status
            if (request.Status != 0 && request.Status != 1)
            {
                return BadRequest(new { success = false, message = "Status không hợp lệ (0=pending, 1=approved)" });
            }

            var result = await _reviewService.UpdateReviewStatusAsync(reviewId, request.Status, adminId.Value);
            
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("Admin {AdminId} updated review {ReviewId} status to {Status}", 
                adminId.Value, reviewId, request.Status);

            return Ok(new { success = true, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi admin cập nhật status review {ReviewId}", reviewId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// User xóa review của mình
    /// DELETE /api/productreviews/{reviewId}
    /// </summary>
    [HttpDelete("{reviewId}")]
    public async Task<IActionResult> DeleteReview(long reviewId)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var result = await _reviewService.DeleteReviewAsync(reviewId, userId.Value);
            
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("User {UserId} deleted review {ReviewId}", userId.Value, reviewId);

            return Ok(new { success = true, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa review {ReviewId}", reviewId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    // ==================== PRIVATE HELPERS ====================

    /// <summary>
    /// Lấy User ID từ JWT claims
    /// </summary>
    private long? GetUserId()
    {
        var userIdClaim = User.FindFirst("uid")?.Value 
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }
}

/// <summary>
/// Request model để admin cập nhật status review
/// </summary>
public class UpdateReviewStatusRequest
{
    public short Status { get; set; } // 0=pending, 1=approved
}

