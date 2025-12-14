using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Services.Interfaces;

namespace WebApp.Controllers;

/// <summary>
/// API Controller cho tài xế quản lý đơn hàng
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "driver")] // Chỉ driver mới truy cập được
public class DriversController : ControllerBase
{
    private readonly IDriverService _driverService;
    private readonly ILogger<DriversController> _logger;

    public DriversController(
        IDriverService driverService,
        ILogger<DriversController> logger)
    {
        _driverService = driverService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách đơn hàng khả dụng (chưa có tài xế nhận)
    /// GET /api/drivers/available-orders
    /// </summary>
    /// <summary>
    /// Lấy danh sách đơn hàng chưa có tài xế (status = 1: Chờ tài xế)
    /// GET /api/drivers/available-orders
    /// </summary>
    /// <summary>
    /// Lấy danh sách đơn hàng chưa có tài xế (status = 1: Chờ tài xế)
    /// GET /api/drivers/available-orders
    /// </summary>
    [HttpGet("available-orders")]
    public async Task<IActionResult> GetAvailableOrders()
    {
        try
        {
            var orders = await _driverService.GetAvailableOrdersAsync();
            return Ok(new { success = true, data = orders });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách đơn khả dụng");
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Lấy danh sách đơn hàng của tài xế hiện tại
    /// GET /api/drivers/my-orders?status=2,3,4
    /// </summary>
    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders([FromQuery] string? status = null)
    {
        try
        {
            // Lấy driver ID từ JWT token
            var driverId = GetDriverId();
            if (driverId == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin tài xế" });
            }

            // Lấy tất cả đơn của driver
            var allOrders = await _driverService.GetMyOrdersAsync(driverId.Value);
            
            // Filter theo status nếu có
            if (!string.IsNullOrEmpty(status))
            {
                var statusFilter = status.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
                    .Where(n => n >= 0)
                    .ToList();
                
                if (statusFilter.Any())
                {
                    allOrders = allOrders.Where(o => statusFilter.Contains(o.Status)).ToList();
                }
            }

            return Ok(new { success = true, data = allOrders });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách đơn của tài xế");
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Lấy chi tiết một đơn hàng
    /// GET /api/drivers/orders/{id}
    /// </summary>
    [HttpGet("orders/{id}")]
    public async Task<IActionResult> GetOrderDetail(long id)
    {
        try
        {
            var driverId = GetDriverId();
            if (driverId == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin tài xế" });
            }

            var order = await _driverService.GetOrderDetailAsync(id, driverId.Value);
            if (order == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            return Ok(new { success = true, data = order });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy chi tiết đơn hàng {OrderId}", id);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Nhận đơn hàng (chuyển từ status 1 → 2)
    /// POST /api/drivers/orders/{id}/accept
    /// </summary>
    [HttpPost("orders/{id}/accept")]
    public async Task<IActionResult> AcceptOrder(long id)
    {
        try
        {
            var driverId = GetDriverId();
            if (driverId == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin tài xế" });
            }

            var result = await _driverService.AcceptOrderAsync(id, driverId.Value);
            
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("Driver {DriverId} đã nhận đơn {OrderId}", driverId.Value, id);
            return Ok(new { success = true, message = result.Message, data = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi nhận đơn hàng {OrderId}", id);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Bắt đầu giao hàng (chuyển từ status 2 → 3: Đang lấy → Đang giao)
    /// POST /api/drivers/orders/{id}/start-delivery
    /// </summary>
    [HttpPost("orders/{id}/start-delivery")]
    public async Task<IActionResult> StartDelivery(long id)
    {
        try
        {
            var driverId = GetDriverId();
            if (driverId == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin tài xế" });
            }

            var result = await _driverService.StartDeliveryAsync(id, driverId.Value);
            
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("Driver {DriverId} bắt đầu giao hàng đơn {OrderId}", driverId.Value, id);
            return Ok(new { success = true, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi bắt đầu giao hàng {OrderId}", id);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Hoàn thành đơn hàng (chuyển từ status 3 → 4: Đang giao → Hoàn thành)
    /// POST /api/drivers/orders/{id}/complete
    /// </summary>
    [HttpPost("orders/{id}/complete")]
    public async Task<IActionResult> CompleteOrder(long id)
    {
        try
        {
            var driverId = GetDriverId();
            if (driverId == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin tài xế" });
            }

            var result = await _driverService.CompleteOrderAsync(id, driverId.Value);
            
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("Driver {DriverId} hoàn thành đơn {OrderId}", driverId.Value, id);
            return Ok(new { success = true, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi hoàn thành đơn hàng {OrderId}", id);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Cập nhật trạng thái tài xế (available/offline/busy)
    /// PUT /api/drivers/status
    /// Body: { "status": "available" }
    /// </summary>
    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateDriverStatusRequest request)
    {
        try
        {
            var driverId = GetDriverId();
            if (driverId == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin tài xế" });
            }

            // Validate status
            var validStatuses = new[] { "available", "offline", "busy" };
            if (!validStatuses.Contains(request.Status?.ToLower()))
            {
                return BadRequest(new { success = false, message = "Trạng thái không hợp lệ. Chỉ chấp nhận: available, offline, busy" });
            }

            var result = await _driverService.UpdateStatusAsync(driverId.Value, request.Status!);
            
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("Driver {DriverId} cập nhật trạng thái: {Status}", driverId.Value, request.Status);
            return Ok(new { success = true, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật trạng thái tài xế");
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Lấy thống kê của tài xế (số đơn, doanh thu, rating)
    /// GET /api/drivers/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var driverId = GetDriverId();
            if (driverId == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin tài xế" });
            }

            var stats = await _driverService.GetDriverStatsAsync(driverId.Value);
            if (stats == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy thông tin tài xế" });
            }

            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thống kê tài xế");
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    // ==================== PRIVATE HELPERS ====================

    /// <summary>
    /// Lấy Driver ID từ JWT claims
    /// </summary>
    /// <summary>
    /// Lấy driver_id từ user_id trong JWT token
    /// </summary>
    private long? GetDriverId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        // Query driver_id từ user_id
        try
        {
            var driverId = _driverService.GetDriverIdByUserId(userId);
            return driverId;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Request model để cập nhật trạng thái tài xế
/// </summary>
public class UpdateDriverStatusRequest
{
    public string? Status { get; set; }
}
