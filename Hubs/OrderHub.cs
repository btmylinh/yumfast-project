using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace WebApp.Hubs
{
    /// <summary>
    /// SignalR Hub để theo dõi trạng thái đơn hàng real-time
    /// </summary>
    public class OrderHub : Hub
    {
        /// <summary>
        /// Tạo tên group cho một đơn hàng
        /// </summary>
        private static string GroupName(long orderId) => $"Order_{orderId}";

        /// <summary>
        /// Client tham gia vào group theo dõi đơn hàng
        /// Frontend gọi: connection.invoke("JoinOrderGroup", orderId)
        /// </summary>
        public async Task JoinOrderGroup(long orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(orderId));
            
            // Có thể gửi message xác nhận đã join
            await Clients.Caller.SendAsync("JoinedOrderGroup", new { orderId, message = "Đã kết nối theo dõi đơn hàng" });
        }

        /// <summary>
        /// Client rời khỏi group theo dõi đơn hàng
        /// Frontend gọi: connection.invoke("LeaveOrderGroup", orderId)
        /// </summary>
        public async Task LeaveOrderGroup(long orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(orderId));
            
            // Có thể gửi message xác nhận đã leave
            await Clients.Caller.SendAsync("LeftOrderGroup", new { orderId, message = "Đã ngừng theo dõi đơn hàng" });
        }

        /// <summary>
        /// Alias cho JoinOrderGroup (để tương thích với code cũ)
        /// </summary>
        public Task SubscribeToOrder(long orderId)
        {
            return JoinOrderGroup(orderId);
        }

        /// <summary>
        /// Alias cho LeaveOrderGroup (để tương thích với code cũ)
        /// </summary>
        public Task UnsubscribeFromOrder(long orderId)
        {
            return LeaveOrderGroup(orderId);
        }

        /// <summary>
        /// Khi client disconnect, tự động leave tất cả groups
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // SignalR tự động remove khỏi groups khi disconnect
            await base.OnDisconnectedAsync(exception);
        }
    }
}

