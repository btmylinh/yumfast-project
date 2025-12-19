using Dapper;
using Npgsql;
using WebApp.Models;
using WebApp.Services.Interfaces;

namespace WebApp.Services;

/// <summary>
/// Service implementation cho Product Review
/// </summary>
public class ProductReviewService : IProductReviewService
{
    private readonly NpgsqlConnection _connection;
    private readonly ILogger<ProductReviewService> _logger;
    
    public ProductReviewService(NpgsqlConnection connection, ILogger<ProductReviewService> logger)
    {
        _connection = connection;
        _logger = logger;
    }
    
    public async Task<bool> CanReviewProductAsync(long productId, long userId)
    {
        var sql = @"
            SELECT 
                EXISTS(
                    SELECT 1 
                    FROM order_items oi
                    JOIN orders o ON o.id = oi.order_id
                    WHERE oi.product_id = @productId 
                      AND o.user_id = @userId
                      AND o.status = 4  -- Status 4 = Hoàn thành (theo OrderStatusHelper)
                ) as has_purchased,
                EXISTS(
                    SELECT 1 
                    FROM product_reviews 
                    WHERE product_id = @productId AND user_id = @userId
                ) as has_review,
                EXISTS(
                    -- Kiểm tra xem đã review qua order chưa
                    SELECT 1 
                    FROM order_reviews or_review
                    JOIN orders o ON o.id = or_review.order_id
                    JOIN order_items oi ON oi.order_id = o.id
                    WHERE oi.product_id = @productId 
                      AND or_review.user_id = @userId
                      AND o.status = 4
                ) as has_order_review";
        
        try
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
            
            var result = await _connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { productId, userId });
            
            if (result == null)
                return false;
            
            // User phải đã mua sản phẩm (có order với status = 4) 
            // và chưa review (cả product_reviews và order_reviews)
            bool hasPurchased = result.has_purchased ?? false;
            bool hasReview = result.has_review ?? false;
            bool hasOrderReview = result.has_order_review ?? false;
            
            return hasPurchased && !hasReview && !hasOrderReview;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking can review product {ProductId} for user {UserId}", productId, userId);
            return false;
        }
    }
    
    public async Task<ServiceResult> CreateReviewAsync(CreateProductReviewViewModel dto, long userId)
    {
        await using var transaction = await _connection.BeginTransactionAsync();
        
        try
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
            
            // Validate can review
            if (!await CanReviewProductAsync(dto.ProductId, userId))
            {
                return ServiceResult.Fail("Không thể đánh giá sản phẩm này (chưa mua hoặc đã đánh giá)");
            }
            
            // Validate rating
            if (dto.Rating < 1 || dto.Rating > 5)
            {
                return ServiceResult.Fail("Rating phải từ 1-5 sao");
            }
            
            // Insert review (status = 0 = pending, cần admin approve)
            var insertSql = @"
                INSERT INTO product_reviews 
                    (product_id, user_id, rating, comment, status, created_at, updated_at)
                VALUES 
                    (@productId, @userId, @rating, @comment, 0, NOW(), NOW())
                RETURNING id";
            
            var reviewId = await _connection.QueryFirstOrDefaultAsync<long>(insertSql, new
            {
                productId = dto.ProductId,
                userId,
                rating = dto.Rating,
                comment = dto.Comment
            }, transaction);
            
            // Update product rating (chỉ tính reviews đã approved)
            await UpdateProductRatingAsync(dto.ProductId, transaction);
            
            await transaction.CommitAsync();
            
            _logger.LogInformation("Product review created for product {ProductId} by user {UserId}", dto.ProductId, userId);
            
            return ServiceResult.Ok("Đánh giá đã được gửi và đang chờ phê duyệt", new { reviewId });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating product review for product {ProductId}", dto.ProductId);
            return ServiceResult.Fail("Không thể tạo đánh giá. Vui lòng thử lại.");
        }
    }
    
    public async Task<ProductReview?> GetProductReviewAsync(long productId, long userId)
    {
        var sql = @"
            SELECT * FROM product_reviews
            WHERE product_id = @productId AND user_id = @userId";
        
        try
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
            
            return await _connection.QueryFirstOrDefaultAsync<ProductReview>(sql, new { productId, userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product review for product {ProductId}, user {UserId}", productId, userId);
            return null;
        }
    }
    
    public async Task<List<ProductReview>> GetProductReviewsAsync(long productId, int page = 1, int pageSize = 10)
    {
        // Lấy reviews từ 2 nguồn:
        // 1. Product reviews trực tiếp (product_reviews), ưu tiên comment riêng; nếu được tạo từ order thì lấy thêm images/comment từ order_reviews qua order_id
        // 2. Reviews từ order reviews (order_reviews) - khi user đánh giá đơn hàng mà chưa có product_reviews tương ứng
        var sql = @"
            SELECT 
                pr.id,
                pr.product_id as ProductId,
                pr.user_id as UserId,
                pr.rating as Rating,
                COALESCE(pr.comment, or_review.comment) as Comment,
                or_review.images as Images,
                pr.status as Status,
                pr.created_at as created_at,
                pr.updated_at as updated_at,
                u.name as user_name,
                'product' as review_source
            FROM product_reviews pr
            LEFT JOIN users u ON u.id = pr.user_id
            LEFT JOIN order_reviews or_review ON or_review.order_id = pr.order_id
            WHERE pr.product_id = @productId 
              AND pr.status = 1  -- Chỉ lấy reviews đã approved
            
            UNION ALL
            
            SELECT 
                or_review.id * -1 as id, -- Negative ID để phân biệt với product_reviews
                oi.product_id as ProductId,
                or_review.user_id as UserId,
                or_review.order_rating as Rating,
                or_review.comment as Comment,
                or_review.images as Images,
                1 as Status, -- Order reviews luôn approved
                or_review.created_at as created_at,
                or_review.updated_at as updated_at,
                u2.name as user_name,
                'order' as review_source
            FROM order_reviews or_review
            JOIN orders o ON o.id = or_review.order_id
            JOIN order_items oi ON oi.order_id = o.id
            LEFT JOIN users u2 ON u2.id = or_review.user_id
            WHERE oi.product_id = @productId
              AND o.status = 4  -- Chỉ lấy từ đơn đã hoàn thành
              AND NOT EXISTS (
                  -- Loại bỏ nếu đã có product_review cho sản phẩm này từ user này
                  SELECT 1 FROM product_reviews pr2 
                  WHERE pr2.product_id = oi.product_id 
                    AND pr2.user_id = or_review.user_id
              )
            
            ORDER BY created_at DESC
            LIMIT @pageSize OFFSET @offset";
        
        try
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
            
            var offset = (page - 1) * pageSize;
            var reviews = await _connection.QueryAsync<dynamic>(sql, new { productId, pageSize, offset });
            
            // Map to ProductReview (Dapper dynamic dùng tên cột lowercase theo Postgres)
            var reviewList = reviews.Select(r => new ProductReview
            {
                Id = (long)(r.id ?? 0L),
                ProductId = (long)(r.productid ?? 0L),
                UserId = (long)(r.userid ?? 0L),
                Rating = (short)(r.rating ?? 0),
                Comment = r.comment,
                Images = r.images as string[] ?? (r.images is IEnumerable<string> imgs ? imgs.ToArray() : Array.Empty<string>()),
                Status = (short)(r.status ?? 0),
                CreatedAt = r.created_at ?? DateTime.UtcNow,
                UpdatedAt = r.updated_at ?? DateTime.UtcNow,
                User = new Models.User { Name = r.user_name ?? "" }
            }).ToList();
            
            return reviewList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for product {ProductId}", productId);
            return new List<ProductReview>();
        }
    }
    
    public async Task<ServiceResult> UpdateReviewStatusAsync(long reviewId, short status, long adminId)
    {
        await using var transaction = await _connection.BeginTransactionAsync();
        
        try
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
            
            // Validate status
            if (status != 0 && status != 1)
            {
                return ServiceResult.Fail("Status không hợp lệ (0=pending, 1=approved)");
            }
            
            // Get review để lấy product_id
            var reviewSql = "SELECT product_id FROM product_reviews WHERE id = @reviewId";
            var review = await _connection.QueryFirstOrDefaultAsync<dynamic>(reviewSql, new { reviewId }, transaction);
            
            if (review == null)
            {
                return ServiceResult.Fail("Không tìm thấy đánh giá");
            }
            
            // Update status
            var updateSql = @"
                UPDATE product_reviews 
                SET status = @status, 
                    updated_at = NOW()
                WHERE id = @reviewId";
            
            await _connection.ExecuteAsync(updateSql, new { reviewId, status }, transaction);
            
            // Update product rating nếu approve (status = 1)
            if (status == 1)
            {
                await UpdateProductRatingAsync(review.product_id, transaction);
            }
            
            await transaction.CommitAsync();
            
            _logger.LogInformation("Admin {AdminId} updated review {ReviewId} status to {Status}", adminId, reviewId, status);
            
            return ServiceResult.Ok(status == 1 ? "Đã phê duyệt đánh giá" : "Đã từ chối đánh giá");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating review {ReviewId} status", reviewId);
            return ServiceResult.Fail("Không thể cập nhật trạng thái đánh giá");
        }
    }
    
    public async Task<ServiceResult> DeleteReviewAsync(long reviewId, long userId)
    {
        await using var transaction = await _connection.BeginTransactionAsync();
        
        try
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
            
            // Check review exists and belongs to user
            var checkSql = "SELECT product_id, user_id FROM product_reviews WHERE id = @reviewId";
            var review = await _connection.QueryFirstOrDefaultAsync<dynamic>(checkSql, new { reviewId }, transaction);
            
            if (review == null)
            {
                return ServiceResult.Fail("Không tìm thấy đánh giá");
            }
            
            if (review.user_id != userId)
            {
                return ServiceResult.Fail("Bạn không có quyền xóa đánh giá này");
            }
            
            // Delete review
            var deleteSql = "DELETE FROM product_reviews WHERE id = @reviewId";
            await _connection.ExecuteAsync(deleteSql, new { reviewId }, transaction);
            
            // Update product rating
            await UpdateProductRatingAsync(review.product_id, transaction);
            
            await transaction.CommitAsync();
            
            _logger.LogInformation("User {UserId} deleted review {ReviewId}", userId, reviewId);
            
            return ServiceResult.Ok("Đã xóa đánh giá");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error deleting review {ReviewId}", reviewId);
            return ServiceResult.Fail("Không thể xóa đánh giá");
        }
    }
    
    public async Task<ProductReviewStats> GetProductReviewStatsAsync(long productId)
    {
        var sql = @"
            SELECT 
                COUNT(*) as total_reviews,
                COALESCE(AVG(rating), 0) as average_rating
            FROM product_reviews
            WHERE product_id = @productId AND status = 1";
        
        try
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
            
            var stats = await _connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { productId });
            
            var result = new ProductReviewStats
            {
                ProductId = productId,
                TotalReviews = (int?)(stats?.total_reviews ?? 0) ?? 0,
                AverageRating = (decimal?)(stats?.average_rating ?? 0) ?? 0m
            };
            
            // Get rating distribution
            var distributionSql = @"
                SELECT rating, COUNT(*) as count
                FROM product_reviews
                WHERE product_id = @productId AND status = 1
                GROUP BY rating
                ORDER BY rating";
            
            var distribution = await _connection.QueryAsync<dynamic>(distributionSql, new { productId });
            
            result.RatingDistribution = new Dictionary<int, int>();
            foreach (var item in distribution)
            {
                result.RatingDistribution[(int)item.rating] = (int)item.count;
            }
            
            // Fill missing ratings with 0
            for (int i = 1; i <= 5; i++)
            {
                if (!result.RatingDistribution.ContainsKey(i))
                {
                    result.RatingDistribution[i] = 0;
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review stats for product {ProductId}", productId);
            return new ProductReviewStats { ProductId = productId };
        }
    }
    
    /// <summary>
    /// Helper method: Cập nhật rating trung bình của sản phẩm (chỉ tính reviews đã approved)
    /// Note: Nếu products table có cột rating, cần update. Hiện tại schema chưa có, nên chỉ log.
    /// </summary>
    private async Task UpdateProductRatingAsync(long productId, NpgsqlTransaction transaction)
    {
        try
        {
            // Tính average rating từ reviews đã approved
            var avgRatingSql = @"
                SELECT COALESCE(AVG(rating), 0) as avg_rating
                FROM product_reviews
                WHERE product_id = @productId AND status = 1";
            
            var avgRating = await _connection.QueryFirstOrDefaultAsync<decimal?>(avgRatingSql, new { productId }, transaction);
            
            // TODO: Nếu products table có cột rating, update ở đây
            // var updateSql = "UPDATE products SET rating = @rating WHERE id = @productId";
            // await _connection.ExecuteAsync(updateSql, new { productId, rating = avgRating ?? 0m }, transaction);
            
            _logger.LogInformation("Product {ProductId} average rating: {Rating}", productId, avgRating ?? 0m);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product rating for product {ProductId}", productId);
            // Không throw, chỉ log lỗi
        }
    }
}

