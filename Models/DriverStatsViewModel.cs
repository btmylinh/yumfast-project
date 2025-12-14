namespace WebApp.Models;

/// <summary>
/// ViewModel thống kê tài xế
/// </summary>
public class DriverStatsViewModel
{
    public long DriverId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    
    // Stats
    public int TotalOrders { get; set; }
    public int TodayOrders { get; set; }
    public int PendingOrders { get; set; }
    public int CompletedOrders { get; set; }
    
    // Earnings (optional - nếu có hoa hồng)
    public int TodayEarnings { get; set; }
    public int MonthEarnings { get; set; }
    
    // Recent activity
    public DateTime? LastOrderCompletedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
}
