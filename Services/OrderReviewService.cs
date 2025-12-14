using Dapper;
using Npgsql;
using WebApp.Models;
using WebApp.Services.Interfaces;

namespace WebApp.Services;

/// <summary>
/// Service implementation cho Order Review
/// </summary>
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
            
            if (result == null)
                return false;
            
            // Validate: order completed (status=5), belongs to user, not reviewed yet
            bool isCompleted = result.status == 5;
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
        await using var transaction = await _connection.BeginTransactionAsync();
        
        try
        {
            // Validate can review
            if (!await CanReviewOrderAsync(dto.OrderId, userId))
                return ServiceResult.Fail("Cannot review this order");
            
            // Get driver_id from order
            var driverIdSql = "SELECT driver_id FROM orders WHERE id = @orderId";
            var driverId = await _connection.QueryFirstOrDefaultAsync<long?>(driverIdSql, new { orderId = dto.OrderId }, transaction);
            
            // Insert review
            var insertSql = @"
                INSERT INTO order_reviews 
                    (order_id, user_id, driver_id, order_rating, driver_rating, comment, images, created_at, updated_at)
                VALUES 
                    (@orderId, @userId, @driverId, @orderRating, @driverRating, @comment, @images, NOW(), NOW())
                RETURNING id";
            
            var reviewId = await _connection.QueryFirstOrDefaultAsync<long>(insertSql, new
            {
                orderId = dto.OrderId,
                userId,
                driverId,
                orderRating = dto.OrderRating,
                driverRating = dto.DriverRating,
                comment = dto.Comment,
                images = dto.Images?.ToArray()
            }, transaction);
            
            // Update driver rating nếu có driver_rating
            if (driverId.HasValue && dto.DriverRating.HasValue)
            {
                await UpdateDriverRatingAsync(driverId.Value, transaction);
            }
            
            await transaction.CommitAsync();
            
            _logger.LogInformation("Review created for order {OrderId} by user {UserId}", dto.OrderId, userId);
            
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
        var sql = @"
            SELECT * FROM order_reviews
            WHERE order_id = @orderId";
        
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
            
            await _connection.ExecuteAsync(sql, new { reviewId, reply });
            
            _logger.LogInformation("Admin {AdminId} replied to review {ReviewId}", adminId, reviewId);
            
            return ServiceResult.Ok("Reply added successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding admin reply to review {ReviewId}", reviewId);
            return ServiceResult.Fail("Failed to add reply");
        }
    }
    
    // Helper method
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
}
