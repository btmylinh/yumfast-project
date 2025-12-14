using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;
using WebApp.Services.Interfaces;
using Npgsql;

namespace WebApp.Controllers;

/// <summary>
/// API Controller để theo dõi trạng thái đơn hàng
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize(AuthenticationSchemes = "Cookies,Bearer")] // Chấp nhận cả Cookie và JWT
public class OrderTrackingController : ControllerBase
{
    private readonly IOrderTrackingService _trackingService;
    private readonly ILogger<OrderTrackingController> _logger;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public OrderTrackingController(
        IOrderTrackingService trackingService,
        ILogger<OrderTrackingController> logger,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        _trackingService = trackingService;
        _logger = logger;
        _config = config;
        _env = env;
    }

    /// <summary>
    /// Lấy thông tin tracking đơn hàng (user chỉ xem đơn của mình, driver/admin xem tất cả)
    /// GET /api/orders/{id}/tracking
    /// </summary>
    [HttpGet("{idOrCode}/tracking")]
    public async Task<IActionResult> GetOrderTracking(string idOrCode)
    {
        try
        {
            _logger.LogInformation("GET /api/orders/{IdOrCode}/tracking - User claims: {Claims}", 
                idOrCode, string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
            
            var userId = GetUserId();
            if (userId == null)
            {
                _logger.LogWarning("Cannot determine userId for order tracking request");
                return Unauthorized(new { success = false, message = "Không xác định được user" });
            }

            var userRole = GetUserRole();
            _logger.LogInformation("User {UserId} with role '{Role}' requesting order {IdOrCode}", 
                userId, userRole, idOrCode);
            
            long? restrictUserId = (userRole == "user") ? userId : null;

            // Resolve id by numeric or by code
            var resolvedId = await ResolveOrderIdAsync(idOrCode, restrictUserId);
            if (resolvedId == null)
            {
                // Log chi tiết từ DB
                await using var debugConn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
                await debugConn.OpenAsync();
                
                string errorDetail = "";
                if (long.TryParse(idOrCode, out var parsedId))
                {
                    await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM orders WHERE id = @id", debugConn);
                    cmd.Parameters.AddWithValue("@id", parsedId);
                    var countResult = await cmd.ExecuteScalarAsync();
                    var exists = countResult != null && Convert.ToInt64(countResult) > 0;
                    errorDetail = exists 
                        ? $"Order {parsedId} exists but access denied (wrong user_id)" 
                        : $"Order {parsedId} does not exist in database";
                }
                else
                {
                    await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM orders WHERE code = @code", debugConn);
                    cmd.Parameters.AddWithValue("@code", idOrCode);
                    var countResult = await cmd.ExecuteScalarAsync();
                    var exists = countResult != null && Convert.ToInt64(countResult) > 0;
                    errorDetail = exists 
                        ? $"Order code '{idOrCode}' exists but access denied (wrong user_id)" 
                        : $"Order code '{idOrCode}' does not exist in database";
                }
                
                _logger.LogWarning("Order identifier {IdOrCode} could not be resolved. Detail: {Detail}", idOrCode, errorDetail);
                return NotFound(new { success = false, message = errorDetail });
            }

            var tracking = await _trackingService.GetOrderTrackingAsync(resolvedId.Value, restrictUserId);
            
            if (tracking == null)
            {
                // Log chi tiết từ service
                await using var debugConn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
                await debugConn.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT id, user_id, code, status FROM orders WHERE id = @id", debugConn);
                cmd.Parameters.AddWithValue("@id", resolvedId.Value);
                await using var reader = await cmd.ExecuteReaderAsync();
                
                string errorDetail = $"Order {resolvedId.Value} not found in GetOrderTrackingAsync";
                if (await reader.ReadAsync())
                {
                    var dbUserId = reader.IsDBNull(1) ? "NULL" : reader.GetInt64(1).ToString();
                    var dbCode = reader.GetString(2);
                    var dbStatus = reader.GetInt16(3);
                    errorDetail = $"Order {resolvedId.Value} (Code={dbCode}, UserId={dbUserId}, Status={dbStatus}) found but service returned null";
                }
                
                _logger.LogWarning("Order {OrderId} tracking is null. Detail: {Detail}", resolvedId, errorDetail);
                return NotFound(new { success = false, message = errorDetail });
            }

            _logger.LogInformation("Successfully retrieved tracking for order {OrderId}", resolvedId);
            return Ok(new { success = true, data = tracking });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to order {IdOrCode}: {Message}", idOrCode, ex.Message);
            return StatusCode(403, new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order not found {IdOrCode}: {Message}", idOrCode, ex.Message);
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy tracking đơn hàng {IdOrCode}: {Message}", idOrCode, ex.Message);
            return StatusCode(500, new { success = false, message = ex.Message, stackTrace = _env.IsDevelopment() ? ex.StackTrace : null });
        }
    }

    /// <summary>
    /// Lấy lịch sử thay đổi trạng thái (timeline)
    /// GET /api/orders/{idOrCode}/history
    /// </summary>
    [HttpGet("{idOrCode}/history")]
    public async Task<IActionResult> GetOrderHistory(string idOrCode)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { success = false, message = "Không xác định được user" });
            }

            var userRole = GetUserRole();
            long? restrictUserId = (userRole == "user") ? userId : null;

            var resolvedId = await ResolveOrderIdAsync(idOrCode, restrictUserId);
            if (resolvedId == null)
            {
                // Log chi tiết từ DB
                await using var debugConn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
                await debugConn.OpenAsync();
                
                string errorDetail = "";
                if (long.TryParse(idOrCode, out var parsedId))
                {
                    await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM orders WHERE id = @id", debugConn);
                    cmd.Parameters.AddWithValue("@id", parsedId);
                    var countResult = await cmd.ExecuteScalarAsync();
                    var exists = countResult != null && Convert.ToInt64(countResult) > 0;
                    errorDetail = exists 
                        ? $"Order {parsedId} exists but access denied" 
                        : $"Order {parsedId} does not exist";
                }
                else
                {
                    await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM orders WHERE code = @code", debugConn);
                    cmd.Parameters.AddWithValue("@code", idOrCode);
                    var countResult = await cmd.ExecuteScalarAsync();
                    var exists = countResult != null && Convert.ToInt64(countResult) > 0;
                    errorDetail = exists 
                        ? $"Order code '{idOrCode}' exists but access denied" 
                        : $"Order code '{idOrCode}' does not exist";
                }
                
                _logger.LogWarning("Order identifier {IdOrCode} could not be resolved for history. Detail: {Detail}", idOrCode, errorDetail);
                return NotFound(new { success = false, message = errorDetail });
            }

            // Kiểm tra quyền truy cập thông qua GetOrderTracking
            var tracking = await _trackingService.GetOrderTrackingAsync(resolvedId.Value, restrictUserId);
            if (tracking == null)
            {
                _logger.LogWarning("Order {OrderId} tracking is null when getting history", resolvedId);
                return NotFound(new { success = false, message = $"Order {resolvedId.Value} not found or access denied" });
            }

            var history = await _trackingService.GetOrderHistoryAsync(resolvedId.Value);
            return Ok(new { success = true, data = history });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy lịch sử đơn hàng {IdOrCode}", idOrCode);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Hủy đơn hàng (chỉ được hủy khi status nhỏ hơn 3)
    /// POST /api/orders/{id}/cancel
    /// Body: { "reason": "Lý do hủy" }
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(long id, [FromBody] CancelOrderRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { success = false, message = "Không xác định được user" });
            }

            // Validate reason
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập lý do hủy đơn" });
            }

            // Kiểm tra xem có được phép hủy không
            var canCancel = await _trackingService.CanCancelOrderAsync(id, userId.Value);
            if (!canCancel)
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Không thể hủy đơn hàng này (đơn đã được tài xế nhận hoặc đang giao)" 
                });
            }

            // Thực hiện hủy đơn
            var result = await _trackingService.CancelOrderAsync(id, userId.Value, request.Reason);
            
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("User {UserId} đã hủy đơn {OrderId}. Lý do: {Reason}", 
                userId.Value, id, request.Reason);

            return Ok(new { success = true, message = "Hủy đơn hàng thành công" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // 403 - Không có quyền hủy đơn này
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi hủy đơn hàng {OrderId}", id);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Admin cập nhật trạng thái đơn hàng (chỉ admin mới được dùng)
    /// PUT /api/orders/{id}/status
    /// Body: { "newStatus": 5, "notes": "Ghi chú" }
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "admin")] // Chỉ admin
    public async Task<IActionResult> UpdateOrderStatus(long id, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            var adminId = GetUserId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Không xác định được admin" });
            }

            // Validate new status
            if (request.NewStatus < 0 || request.NewStatus > 7)
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Trạng thái không hợp lệ. Giá trị hợp lệ: 0-7" 
                });
            }

            var result = await _trackingService.UpdateOrderStatusAsync(
                id, 
                request.NewStatus, 
                request.Notes ?? "Admin cập nhật trạng thái", 
                adminId.Value
            );
            
            if (!result.Success)
            {
                return BadRequest(new { 
                    success = false, 
                    message = result.Message 
                });
            }

            _logger.LogInformation("Admin {AdminId} cập nhật trạng thái đơn {OrderId} → {NewStatus}", 
                adminId.Value, id, request.NewStatus);

            return Ok(new { success = true, message = "Cập nhật trạng thái thành công" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi admin cập nhật trạng thái đơn {OrderId}", id);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    // ==================== PRIVATE HELPERS ====================

    private async Task<long?> ResolveOrderIdAsync(string idOrCode, long? restrictUserId)
    {
        await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();

        // Nếu là số -> tìm theo id, nếu không -> tìm theo code
        long? resolved = null;
        if (long.TryParse(idOrCode, out var id))
        {
            // Kiểm tra order có tồn tại không (không filter user_id)
            await using var checkCmd = new NpgsqlCommand("SELECT id, user_id, code FROM orders WHERE id = @id", conn);
            checkCmd.Parameters.AddWithValue("@id", id);
            await using var checkReader = await checkCmd.ExecuteReaderAsync();
            
            if (await checkReader.ReadAsync())
            {
                var dbOrderId = checkReader.GetInt64(0);
                var dbUserId = checkReader.IsDBNull(1) ? (long?)null : checkReader.GetInt64(1);
                var dbCode = checkReader.IsDBNull(2) ? "" : checkReader.GetString(2);
                
                _logger.LogInformation("Order {OrderId} found in DB: UserId={DbUserId}, Code={DbCode}, RequestUserId={RequestUserId}", 
                    dbOrderId, dbUserId, dbCode, restrictUserId);
                
                // Kiểm tra quyền truy cập
                if (restrictUserId.HasValue && dbUserId.HasValue && dbUserId.Value != restrictUserId.Value)
                {
                    _logger.LogWarning("Access denied: Order {OrderId} belongs to UserId={DbUserId}, but UserId={RequestUserId} requested", 
                        dbOrderId, dbUserId.Value, restrictUserId.Value);
                    return null; // Không có quyền
                }
                
                resolved = dbOrderId;
            }
            else
            {
                _logger.LogWarning("Order {OrderId} NOT FOUND in database", id);
                // Debug: List recent orders
                await checkReader.CloseAsync();
                await using var debugCmd = new NpgsqlCommand("SELECT id, code, user_id FROM orders ORDER BY id DESC LIMIT 10", conn);
                await using var debugReader = await debugCmd.ExecuteReaderAsync();
                var orders = new List<string>();
                while (await debugReader.ReadAsync())
                {
                    orders.Add($"ID={debugReader.GetInt64(0)} Code={debugReader.GetString(1)} UserId={(debugReader.IsDBNull(2) ? "NULL" : debugReader.GetInt64(2).ToString())}");
                }
                _logger.LogInformation("Recent orders in DB: {Orders}", string.Join("; ", orders));
            }
        }
        else
        {
            // Tìm theo code
            await using var checkCmd = new NpgsqlCommand("SELECT id, user_id, code FROM orders WHERE code = @code", conn);
            checkCmd.Parameters.AddWithValue("@code", idOrCode);
            await using var checkReader = await checkCmd.ExecuteReaderAsync();
            
            if (await checkReader.ReadAsync())
            {
                var dbOrderId = checkReader.GetInt64(0);
                var dbUserId = checkReader.IsDBNull(1) ? (long?)null : checkReader.GetInt64(1);
                var dbCode = checkReader.GetString(2);
                
                _logger.LogInformation("Order code '{Code}' found in DB: OrderId={OrderId}, UserId={DbUserId}, RequestUserId={RequestUserId}", 
                    idOrCode, dbOrderId, dbUserId, restrictUserId);
                
                if (restrictUserId.HasValue && dbUserId.HasValue && dbUserId.Value != restrictUserId.Value)
                {
                    _logger.LogWarning("Access denied: Order code '{Code}' (ID={OrderId}) belongs to UserId={DbUserId}, but UserId={RequestUserId} requested", 
                        idOrCode, dbOrderId, dbUserId.Value, restrictUserId.Value);
                    return null;
                }
                
                resolved = dbOrderId;
            }
            else
            {
                _logger.LogWarning("Order code '{Code}' NOT FOUND in database", idOrCode);
            }
        }
        
        return resolved;
    }

    /// <summary>
    /// Lấy User ID từ JWT claims
    /// </summary>
    private long? GetUserId()
    {
        // Đọc lần lượt theo các key có thể xuất hiện khi đăng nhập bằng Cookie/JWT
        var id =
            User.FindFirstValue("uid") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        return long.TryParse(id, out var userId) ? userId : (long?)null;
    }

    /// <summary>
    /// Lấy User Role từ claims (ưu tiên ClaimTypes.Role)
    /// </summary>
    private string GetUserRole()
    {
        var role =
            User.FindFirstValue(ClaimTypes.Role) ??
            User.FindFirstValue("role") ??
            User.FindFirstValue("roles");

        return (role ?? "user").ToLowerInvariant();
    }
}

/// <summary>
/// Request model để hủy đơn hàng
/// </summary>
public class CancelOrderRequest
{
    public string? Reason { get; set; }
}

/// <summary>
/// Request model để admin cập nhật trạng thái
/// </summary>
public class UpdateOrderStatusRequest
{
    public int NewStatus { get; set; }
    public string? Notes { get; set; }
}
