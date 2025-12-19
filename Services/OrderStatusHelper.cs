using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Helper class để đồng bộ status mapping giữa database và display
    /// Display status: 1=Chờ tài xế, 2=Đang lấy đồ ăn, 3=Đang giao hàng, 4=Hoàn thành, 5=Đã hủy, 6=Đã hoàn tiền
    /// </summary>
    public static class OrderStatusHelper
    {
        /// <summary>
        /// Lấy text hiển thị theo display status (chuẩn OrderTrackingService)
        /// </summary>
        public static string GetStatusText(int displayStatus)
        {
            return displayStatus switch
            {
                1 => "Chờ tài xế",          // Đã xác nhận, đang chờ tài xế nhận đơn
                2 => "Đang lấy đồ ăn",      // Tài xế đã nhận và đang đến lấy hàng
                3 => "Đang giao hàng",      // Tài xế đang giao đến khách
                4 => "Hoàn thành",          // Đã giao thành công
                5 => "Đã hủy",              // Đơn hàng bị hủy
                6 => "Đã hoàn tiền",        // Đã hoàn tiền cho khách
                _ => "Không xác định"
            };
        }
    
        
        /// <summary>
        /// Lấy màu Bootstrap theo display status
        /// </summary>
        public static string GetStatusColor(int displayStatus)
        {
            return displayStatus switch
            {
                1 => "info",      // Chờ tài xế - xanh dương nhạt
                2 => "primary",   // Đang lấy đồ ăn - xanh dương
                3 => "primary",   // Đang giao hàng - xanh dương
                4 => "success",   // Hoàn thành - xanh lá
                5 => "danger",    // Đã hủy - đỏ
                6 => "warning",   // Đã hoàn tiền - vàng
                _ => "secondary"
            };
        }
        
        /// <summary>
        /// Lấy icon Bootstrap theo display status
        /// </summary>
        public static string GetStatusIcon(int displayStatus)
        {
            return displayStatus switch
            {
                1 => "bi-clock-history",        // Chờ tài xế
                2 => "bi-box-arrow-in-down",    // Đang lấy đồ ăn
                3 => "bi-truck",                 // Đang giao hàng
                4 => "bi-check-circle",         // Hoàn thành
                5 => "bi-x-circle",             // Đã hủy
                6 => "bi-arrow-counterclockwise", // Đã hoàn tiền
                _ => "bi-hourglass-split"
            };
        }
        
        /// <summary>
        /// Kiểm tra đơn hàng có thể hủy không (theo database status)
        /// </summary>
        public static bool CanCancel(int dbStatus)
        {
            // Chỉ hủy được khi status = 0 (pending) hoặc 1 (confirmed - chờ tài xế)
            return dbStatus == 0 || dbStatus == 1;
        }
        
        /// <summary>
        /// Kiểm tra đơn hàng có thể đánh giá không (theo database status)
        /// </summary>
        public static bool CanReview(int dbStatus)
        {
            // Chỉ đánh giá được khi status = 4 (Hoàn thành) - theo OrderStatusHelper mới
            return dbStatus == 4;
        }
        
        /// <summary>
        /// Kiểm tra đơn hàng đang active (chưa hoàn thành, chưa hủy, chưa hoàn tiền)
        /// </summary>
        public static bool IsActive(int dbStatus)
        {
            // Active: 0, 1, 2, 3, 4
            // Inactive: 5 (completed), 6 (cancelled), 7 (refunded)
            return dbStatus >= 0 && dbStatus <= 4;
        }
    }
}

