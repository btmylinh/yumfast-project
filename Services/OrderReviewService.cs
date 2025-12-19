using Dapper;
using Npgsql;
using WebApp.Models;
using WebApp.Services.Interfaces;

namespace WebApp.Services;

public class OrderReviewService : IOrderReviewService
{
    private readonly NpgsqlConnection _connection;
    private readonly ILogger<OrderReviewService> _logger;

    public OrderReviewService(NpgsqlConnection connection, ILogger<OrderReviewService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<bool> CanReviewOrderAsync(long orderId, long userId)
    {
        var sql = @"
            SELECT o.status, o.user_id,
                   EXISTS(SELECT 1 FROM order_reviews WHERE order_id = @orderId) as has_review
            FROM orders o
            WHERE o.id = @orderId";

        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { orderId });
            if (result == null) return false;

            bool isCompleted = result.status == 4;
            bool isOwner = result.user_id == userId;
            bool hasReview = result.has_review;

            return isCompleted && isOwner && !hasReview;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking can review order {OrderId}", orderId);
            return false;
        }
    }

    public async Task<ServiceResult> CreateReviewAsync(CreateReviewViewModel dto, long userId)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        await using var transaction = await _connection.BeginTransactionAsync();

        try
        {
            if (!await CanReviewOrderAsync(dto.OrderId, userId))
                return ServiceResult.Fail("Cannot review this order");

            var driverIdSql = "SELECT driver_id FROM orders WHERE id = @orderId";
            var driverId = await _connection.QueryFirstOrDefaultAsync<long?>(
                driverIdSql, new { orderId = dto.OrderId }, transaction);

            var insertSql = @"
                INSERT INTO order_reviews
                    (order_id, user_id, driver_id, order_rating, driver_rating, comment, images, created_at, updated_at)
                VALUES
                    (@orderId, @userId, @driverId, @orderRating, @driverRating, @comment, @images, NOW(), NOW())
                RETURNING id";

            var reviewId = await _connection.QueryFirstAsync<long>(insertSql, new
            {
                orderId = dto.OrderId,
                userId,
                driverId,
                orderRating = dto.OrderRating,
                driverRating = dto.DriverRating,
                comment = dto.Comment,
                images = dto.Images?.ToArray()
            }, transaction);

            if (driverId.HasValue && dto.DriverRating.HasValue)
                await UpdateDriverRatingAsync(driverId.Value, transaction);

            // ✅ FIX: tạo product_reviews có order_id để GET details trả list đúng
            await CreateProductReviewsFromOrderAsync(dto.OrderId, userId, dto.OrderRating, dto.Comment, transaction);

            await transaction.CommitAsync();
            return ServiceResult.Ok("Review created successfully", new { reviewId });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating review for order {OrderId}", dto.OrderId);
            return ServiceResult.Fail("Failed to create review");
        }
    }

    public async Task<OrderReview?> GetOrderReviewAsync(long orderId)
    {
        var sql = @"SELECT * FROM order_reviews WHERE order_id = @orderId";
        try
        {
            return await _connection.QueryFirstOrDefaultAsync<OrderReview>(sql, new { orderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review for order {OrderId}", orderId);
            return null;
        }
    }

    public async Task<object?> GetOrderReviewDetailsAsync(long orderId)
    {
        try
        {
            var orderReviewSql = @"
                SELECT
                    or_review.*,
                    u.name as user_name,
                    u.avatar as user_avatar,
                    d.name as driver_name
                FROM order_reviews or_review
                JOIN users u ON u.id = or_review.user_id
                LEFT JOIN drivers d ON d.id = or_review.driver_id
                WHERE or_review.order_id = @orderId";

            var orderReview = await _connection.QueryFirstOrDefaultAsync<dynamic>(orderReviewSql, new { orderId });
            if (orderReview == null) return null;

            var productReviewsSql = @"
                SELECT
                    pr.*,
                    p.name as product_name,
                    p.images as product_images
                FROM product_reviews pr
                JOIN products p ON p.id = pr.product_id
                WHERE pr.order_id = @orderId
                ORDER BY pr.created_at ASC";

            var productReviews = (await _connection.QueryAsync<dynamic>(productReviewsSql, new { orderId })).ToList();

            var avgProductRating = productReviews.Any()
                ? productReviews.Average(pr => (decimal)pr.rating)
                : 0;

            return new
            {
                orderReview = new
                {
                    id = orderReview.id,
                    orderId = orderReview.order_id,
                    userId = orderReview.user_id,
                    userName = orderReview.user_name,
                    userAvatar = orderReview.user_avatar,
                    driverId = orderReview.driver_id,
                    driverName = orderReview.driver_name,
                    orderRating = orderReview.order_rating,
                    driverRating = orderReview.driver_rating,
                    comment = orderReview.comment,
                    images = orderReview.images,
                    adminReply = orderReview.admin_reply,
                    adminRepliedAt = orderReview.admin_replied_at,
                    createdAt = orderReview.created_at,
                    updatedAt = orderReview.updated_at
                },
                productReviews = productReviews.Select(pr => new
                {
                    id = pr.id,
                    productId = pr.product_id,
                    productName = pr.product_name,
                    productImages = pr.product_images,
                    rating = pr.rating,
                    comment = pr.comment,
                    status = pr.status,
                    createdAt = pr.created_at
                }).ToList(),
                stats = new
                {
                    totalProducts = productReviews.Count,
                    averageProductRating = Math.Round(avgProductRating, 1),
                    orderRating = orderReview.order_rating,
                    driverRating = orderReview.driver_rating
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review details for order {OrderId}", orderId);
            return null;
        }
    }

    public async Task<List<OrderReview>> GetDriverReviewsAsync(long driverId, int page = 1, int pageSize = 10)
    {
        var sql = @"
            SELECT * FROM order_reviews
            WHERE driver_id = @driverId
            ORDER BY created_at DESC
            LIMIT @pageSize OFFSET @offset";

        try
        {
            var offset = (page - 1) * pageSize;
            var reviews = await _connection.QueryAsync<OrderReview>(sql, new { driverId, pageSize, offset });
            return reviews.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for driver {DriverId}", driverId);
            return new List<OrderReview>();
        }
    }

    // ✅ FIX: reply theo ORDER_ID (đúng với route controller)
    public async Task<ServiceResult> AdminReplyByOrderIdAsync(long orderId, string reply, long adminId)
    {
        try
        {
            var sql = @"
                UPDATE order_reviews
                SET admin_reply = @reply,
                    admin_replied_at = NOW(),
                    updated_at = NOW()
                WHERE order_id = @orderId";

            var affected = await _connection.ExecuteAsync(sql, new { orderId, reply });

            if (affected == 0)
                return ServiceResult.Fail("Review not found");

            _logger.LogInformation("Admin {AdminId} replied to order review {OrderId}", adminId, orderId);
            return ServiceResult.Ok("Reply added successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding admin reply for order {OrderId}", orderId);
            return ServiceResult.Fail("Failed to add reply");
        }
    }

    private async Task UpdateDriverRatingAsync(long driverId, NpgsqlTransaction transaction)
    {
        var sql = @"
            UPDATE drivers
            SET rating = (
                SELECT AVG(driver_rating)
                FROM order_reviews
                WHERE driver_id = @driverId AND driver_rating IS NOT NULL
            ),
            updated_at = NOW()
            WHERE id = @driverId";

        await _connection.ExecuteAsync(sql, new { driverId }, transaction);
    }

    // ✅ FIX: set order_id khi insert product_reviews
    private async Task CreateProductReviewsFromOrderAsync(
        long orderId,
        long userId,
        int orderRating,
        string? orderComment,
        NpgsqlTransaction transaction)
    {
        try
        {
            var productsSql = @"
                SELECT DISTINCT oi.product_id
                FROM order_items oi
                WHERE oi.order_id = @orderId";

            var productIds = await _connection.QueryAsync<long>(productsSql, new { orderId }, transaction);

            foreach (var productId in productIds)
            {
                // (Optional) tránh tạo trùng nếu chạy lại
                var existsSql = @"
                    SELECT EXISTS(
                        SELECT 1 FROM product_reviews
                        WHERE order_id = @orderId AND product_id = @productId AND user_id = @userId
                    )";
                var exists = await _connection.QueryFirstAsync<bool>(existsSql, new { orderId, productId, userId }, transaction);
                if (exists) continue;

                var insertProductReviewSql = @"
                    INSERT INTO product_reviews
                        (order_id, product_id, user_id, rating, comment, status, created_at, updated_at)
                    VALUES
                        (@orderId, @productId, @userId, @rating, @comment, 1, NOW(), NOW())";

                await _connection.ExecuteAsync(insertProductReviewSql, new
                {
                    orderId,
                    productId,
                    userId,
                    rating = orderRating,
                    comment = orderComment
                }, transaction);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product reviews from order {OrderId}", orderId);
            // không throw để tránh rollback order review
        }
    }

    public async Task<ServiceResult> AdminReplyAsync(long reviewId, string reply, long adminId)
    {
        try
        {
            var sql = @"
                UPDATE order_reviews
                SET admin_reply = @reply,
                    admin_replied_at = NOW(),
                    updated_at = NOW()
                WHERE id = @reviewId";

            var affected = await _connection.ExecuteAsync(sql, new { reviewId, reply });

            if (affected == 0)
                return ServiceResult.Fail("Review not found");

            _logger.LogInformation("Admin {AdminId} replied to review {ReviewId}", adminId, reviewId);
            return ServiceResult.Ok("Reply added successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding admin reply to review {ReviewId}", reviewId);
            return ServiceResult.Fail("Failed to add reply");
        }
    }
}
