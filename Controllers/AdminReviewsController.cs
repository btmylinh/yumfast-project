using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Services.Interfaces;
using WebApp.Models;
using Dapper;
using Npgsql;

namespace WebApp.Controllers;

/// <summary>
/// API Controller quản lý reviews cho Admin
/// </summary>
[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = "admin")] // Chỉ admin
public class AdminReviewsController : ControllerBase
{
    private readonly IOrderReviewService _orderReviewService;
    private readonly IProductReviewService _productReviewService;
    private readonly IConfiguration _config;
    private readonly ILogger<AdminReviewsController> _logger;

    public AdminReviewsController(
        IOrderReviewService orderReviewService,
        IProductReviewService productReviewService,
        IConfiguration config,
        ILogger<AdminReviewsController> logger)
    {
        _orderReviewService = orderReviewService;
        _productReviewService = productReviewService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Lấy tất cả reviews với filter
    /// GET /api/admin/reviews?type=order&status=1&rating=5&page=1&pageSize=20
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReviews(
        [FromQuery] string? type, // "order" hoặc "product"
        [FromQuery] int? status, // 0=pending, 1=approved (chỉ cho product)
        [FromQuery] int? rating, // 1-5
        [FromQuery] string? search, // Tìm kiếm theo product name, user name, comment
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            if (type == "order")
            {
                return await GetOrderReviews(conn, rating, search, fromDate, toDate, page, pageSize);
            }
            else if (type == "product")
            {
                return await GetProductReviews(conn, status, rating, search, fromDate, toDate, page, pageSize);
            }
            else
            {
                // Lấy cả 2 loại
                var orderReviews = await GetOrderReviewsData(conn, rating, search, fromDate, toDate, page, pageSize);
                var productReviews = await GetProductReviewsData(conn, status, rating, search, fromDate, toDate, page, pageSize);
                
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        orderReviews = orderReviews.reviews,
                        productReviews = productReviews.reviews,
                        pagination = new
                        {
                            page,
                            pageSize,
                            totalOrderReviews = orderReviews.total,
                            totalProductReviews = productReviews.total,
                            totalPages = (int)Math.Ceiling((orderReviews.total + productReviews.total) / (double)pageSize)
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for admin");
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    private async Task<IActionResult> GetOrderReviews(NpgsqlConnection conn, int? rating, string? search, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var result = await GetOrderReviewsData(conn, rating, search, fromDate, toDate, page, pageSize);
        return Ok(new
        {
            success = true,
            data = result.reviews,
            pagination = new
            {
                page,
                pageSize,
                total = result.total,
                totalPages = (int)Math.Ceiling(result.total / (double)pageSize)
            }
        });
    }

    private async Task<IActionResult> GetProductReviews(NpgsqlConnection conn, int? status, int? rating, string? search, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var result = await GetProductReviewsData(conn, status, rating, search, fromDate, toDate, page, pageSize);
        return Ok(new
        {
            success = true,
            data = result.reviews,
            pagination = new
            {
                page,
                pageSize,
                total = result.total,
                totalPages = (int)Math.Ceiling(result.total / (double)pageSize)
            }
        });
    }

    private async Task<(List<object> reviews, long total)> GetOrderReviewsData(NpgsqlConnection conn, int? rating, string? search, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var where = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (rating.HasValue)
        {
            where += " AND or_review.order_rating = @rating";
            parameters.Add("rating", rating.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (o.code ILIKE @search OR u.name ILIKE @search OR or_review.comment ILIKE @search)";
            parameters.Add("search", $"%{search}%");
        }

        if (fromDate.HasValue)
        {
            where += " AND or_review.created_at >= @fromDate";
            parameters.Add("fromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            where += " AND or_review.created_at <= @toDate";
            parameters.Add("toDate", toDate.Value.AddDays(1)); // Include the whole day
        }

        // Count
        var countSql = $@"
            SELECT COUNT(*)
            FROM order_reviews or_review
            JOIN orders o ON o.id = or_review.order_id
            LEFT JOIN users u ON u.id = or_review.user_id
            {where}";

        var total = await conn.QueryFirstOrDefaultAsync<long>(countSql, parameters);

        // Data
        var sql = $@"
            SELECT 
                or_review.id,
                or_review.order_id as OrderId,
                o.code as OrderCode,
                or_review.user_id as UserId,
                u.name as UserName,
                or_review.order_rating as OrderRating,
                or_review.driver_rating as DriverRating,
                or_review.comment as Comment,
                or_review.admin_reply as AdminReply,
                or_review.admin_replied_at as AdminRepliedAt,
                or_review.created_at as CreatedAt
            FROM order_reviews or_review
            JOIN orders o ON o.id = or_review.order_id
            LEFT JOIN users u ON u.id = or_review.user_id
            {where}
            ORDER BY or_review.created_at DESC
            LIMIT @limit OFFSET @offset";

        parameters.Add("limit", pageSize);
        parameters.Add("offset", (page - 1) * pageSize);

        var reviews = await conn.QueryAsync(sql, parameters);

        var reviewList = reviews.Select(r => new
        {
            id = r.id,
            type = "order",
            orderId = r.OrderId,
            orderCode = r.OrderCode,
            userId = r.UserId,
            userName = r.UserName ?? "Người dùng",
            orderRating = r.OrderRating,
            driverRating = r.DriverRating,
            comment = r.Comment,
            adminReply = r.AdminReply,
            adminRepliedAt = r.AdminRepliedAt,
            createdAt = r.CreatedAt
        }).ToList<object>();

        return (reviewList, total);
    }

    private async Task<(List<object> reviews, long total)> GetProductReviewsData(NpgsqlConnection conn, int? status, int? rating, string? search, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var where = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (status.HasValue)
        {
            where += " AND pr.status = @status";
            parameters.Add("status", status.Value);
        }

        if (rating.HasValue)
        {
            where += " AND pr.rating = @rating";
            parameters.Add("rating", rating.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (p.name ILIKE @search OR u.name ILIKE @search OR pr.comment ILIKE @search)";
            parameters.Add("search", $"%{search}%");
        }

        if (fromDate.HasValue)
        {
            where += " AND pr.created_at >= @fromDate";
            parameters.Add("fromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            where += " AND pr.created_at <= @toDate";
            parameters.Add("toDate", toDate.Value.AddDays(1));
        }

        // Count
        var countSql = $@"
            SELECT COUNT(*)
            FROM product_reviews pr
            JOIN products p ON p.id = pr.product_id
            LEFT JOIN users u ON u.id = pr.user_id
            {where}";

        var total = await conn.QueryFirstOrDefaultAsync<long>(countSql, parameters);

        // Data
        var sql = $@"
            SELECT 
                pr.id,
                pr.product_id as ProductId,
                p.name as ProductName,
                pr.user_id as UserId,
                u.name as UserName,
                pr.rating as Rating,
                pr.comment as Comment,
                pr.status as Status,
                pr.created_at as CreatedAt,
                pr.updated_at as UpdatedAt
            FROM product_reviews pr
            JOIN products p ON p.id = pr.product_id
            LEFT JOIN users u ON u.id = pr.user_id
            {where}
            ORDER BY pr.created_at DESC
            LIMIT @limit OFFSET @offset";

        parameters.Add("limit", pageSize);
        parameters.Add("offset", (page - 1) * pageSize);

        var reviews = await conn.QueryAsync(sql, parameters);

        var reviewList = reviews.Select(r => new
        {
            id = r.id,
            type = "product",
            productId = r.ProductId,
            productName = r.ProductName,
            userId = r.UserId,
            userName = r.UserName ?? "Người dùng",
            rating = r.Rating,
            comment = r.Comment,
            status = r.Status,
            statusText = r.Status == 1 ? "Đã phê duyệt" : "Chờ phê duyệt",
            createdAt = r.CreatedAt,
            updatedAt = r.UpdatedAt
        }).ToList<object>();

        return (reviewList, total);
    }

    /// <summary>
    /// Lấy thống kê reviews
    /// GET /api/admin/reviews/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            // Order reviews stats
            var orderStatsSql = @"
                SELECT 
                    COUNT(*) as Total,
                    AVG(order_rating) as AvgRating,
                    COUNT(*) FILTER (WHERE order_rating = 5) as Rating5,
                    COUNT(*) FILTER (WHERE order_rating = 4) as Rating4,
                    COUNT(*) FILTER (WHERE order_rating = 3) as Rating3,
                    COUNT(*) FILTER (WHERE order_rating = 2) as Rating2,
                    COUNT(*) FILTER (WHERE order_rating = 1) as Rating1
                FROM order_reviews";

            var orderStats = await conn.QueryFirstOrDefaultAsync<dynamic>(orderStatsSql);

            // Product reviews stats
            var productStatsSql = @"
                SELECT 
                    COUNT(*) as Total,
                    AVG(rating) as AvgRating,
                    COUNT(*) FILTER (WHERE status = 1) as Approved,
                    COUNT(*) FILTER (WHERE status = 0) as Pending,
                    COUNT(*) FILTER (WHERE rating = 5) as Rating5,
                    COUNT(*) FILTER (WHERE rating = 4) as Rating4,
                    COUNT(*) FILTER (WHERE rating = 3) as Rating3,
                    COUNT(*) FILTER (WHERE rating = 2) as Rating2,
                    COUNT(*) FILTER (WHERE rating = 1) as Rating1
                FROM product_reviews";

            var productStats = await conn.QueryFirstOrDefaultAsync<dynamic>(productStatsSql);

            return Ok(new
            {
                success = true,
                data = new
                {
                    orderReviews = new
                    {
                        total = (long)(orderStats?.Total ?? 0),
                        averageRating = orderStats?.AvgRating != null ? Math.Round((decimal)orderStats.AvgRating, 2) : 0,
                        ratingDistribution = new Dictionary<int, long>
                        {
                            { 5, (long)(orderStats?.Rating5 ?? 0) },
                            { 4, (long)(orderStats?.Rating4 ?? 0) },
                            { 3, (long)(orderStats?.Rating3 ?? 0) },
                            { 2, (long)(orderStats?.Rating2 ?? 0) },
                            { 1, (long)(orderStats?.Rating1 ?? 0) }
                        }
                    },
                    productReviews = new
                    {
                        total = (long)(productStats?.Total ?? 0),
                        approved = (long)(productStats?.Approved ?? 0),
                        pending = (long)(productStats?.Pending ?? 0),
                        averageRating = productStats?.AvgRating != null ? Math.Round((decimal)productStats.AvgRating, 2) : 0,
                        ratingDistribution = new Dictionary<int, long>
                        {
                            { 5, (long)(productStats?.Rating5 ?? 0) },
                            { 4, (long)(productStats?.Rating4 ?? 0) },
                            { 3, (long)(productStats?.Rating3 ?? 0) },
                            { 2, (long)(productStats?.Rating2 ?? 0) },
                            { 1, (long)(productStats?.Rating1 ?? 0) }
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review stats");
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Approve/Reject product review
    /// PUT /api/admin/reviews/product/{reviewId}/status
    /// Body: { "status": 1 } // 0=pending, 1=approved
    /// </summary>
    [HttpPut("product/{reviewId}/status")]
    public async Task<IActionResult> UpdateProductReviewStatus(long reviewId, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            var adminId = GetUserId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Không xác định được admin" });
            }

            var result = await _productReviewService.UpdateReviewStatusAsync(reviewId, request.Status, adminId.Value);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product review {ReviewId} status", reviewId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Trả lời order review (delegate to OrderReviewsController logic)
    /// POST /api/admin/reviews/order/{orderId}/reply
    /// Body: { "reply": "Cảm ơn bạn đã đánh giá..." }
    /// </summary>
    [HttpPost("order/{orderId}/reply")]
    public async Task<IActionResult> ReplyOrderReview(long orderId, [FromBody] AdminReplyRequest request)
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

            // OrderReviewService.AdminReplyAsync nhận reviewId, nhưng ta có orderId
            // Cần lấy reviewId từ orderId
            var review = await _orderReviewService.GetOrderReviewAsync(orderId);
            if (review == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đánh giá" });
            }
            
            var result = await _orderReviewService.AdminReplyAsync(review.Id, request.Reply, adminId.Value);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = "Trả lời đánh giá thành công" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replying to order review {OrderId}", orderId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Xóa product review
    /// DELETE /api/admin/reviews/product/{reviewId}
    /// </summary>
    [HttpDelete("product/{reviewId}")]
    public async Task<IActionResult> DeleteProductReview(long reviewId)
    {
        try
        {
            var adminId = GetUserId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Không xác định được admin" });
            }

            // Admin có thể xóa bất kỳ review nào
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var deleteSql = "DELETE FROM product_reviews WHERE id = @reviewId";
            var deleted = await conn.ExecuteAsync(deleteSql, new { reviewId });

            if (deleted == 0)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đánh giá" });
            }

            return Ok(new { success = true, message = "Đã xóa đánh giá" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product review {ReviewId}", reviewId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

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
/// Request model để update review status
/// </summary>
public class UpdateStatusRequest
{
    public short Status { get; set; } // 0=pending, 1=approved
}

/// <summary>
/// Request model để admin trả lời review
/// </summary>
public class AdminReplyRequest
{
    public string Reply { get; set; } = string.Empty;
}

