// Alpine component for Order Detail Page
// Usage: <div x-data="orderDetail({ orderId: 123 })" x-init="init()">

function orderDetail(initial) {
  return {
    orderId: initial?.orderId || null,
    order: null,
    loading: false,
    error: '',
    
    // Realtime updates
    hubConnection: null,
    isConnected: false,
    
    // Status timeline
    statusTimeline: [],
    
    // Status mapping
    statusMap: {
      'pending': { label: 'Chờ xác nhận', badge: 'warning', icon: 'clock' },
      'confirmed': { label: 'Đã xác nhận', badge: 'info', icon: 'check-circle' },
      'shipped': { label: 'Đang giao', badge: 'primary', icon: 'truck' },
      'delivered': { label: 'Đã giao', badge: 'success', icon: 'check-circle' },
      'cancelled': { label: 'Đã hủy', badge: 'danger', icon: 'x-circle' }
    },

    fmt(v) { 
      try { 
        return '₫' + Number(v || 0).toLocaleString('vi-VN'); 
      } catch { 
        return v; 
      } 
    },

    formatDate(dateStr) {
      if (!dateStr) return '';
      const date = new Date(dateStr);
      return date.toLocaleDateString('vi-VN', { 
        year: 'numeric', 
        month: '2-digit', 
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });
    },

    getStatusInfo(status) {
      return this.statusMap[status] || { label: status, badge: 'secondary', icon: 'info-circle' };
    },

    async init() {
      // Kiểm tra đăng nhập
      if (typeof authHelper !== 'undefined' && !authHelper.isLoggedIn()) {
        authHelper.requireLogin(`/user/orders/${this.orderId}`);
        return;
      }

      if (!this.orderId) {
        this.error = 'Không tìm thấy ID đơn hàng';
        return;
      }

      await this.loadOrderDetail();
      
      // Kết nối SignalR để nhận realtime updates
      if (typeof orderHub !== 'undefined') {
        await this.connectToHub();
      }
    },

    async loadOrderDetail() {
      this.loading = true;
      this.error = '';
      try {
        const res = await fetch(`/api/orders/${this.orderId}`);
        if (!res.ok) throw new Error('load_failed');
        
        const json = await res.json();
        this.order = json.data || null;
        
        if (!this.order) {
          this.error = 'Không tìm thấy đơn hàng';
          return;
        }

        // Build timeline from order status history
        this.buildTimeline();
      } catch (e) {
        console.error('Load order detail error:', e);
        this.error = 'Không thể tải chi tiết đơn hàng';
      } finally {
        this.loading = false;
      }
    },

    buildTimeline() {
      if (!this.order) return;

      this.statusTimeline = [];

      // Add creation event
      this.statusTimeline.push({
        status: 'created',
        label: 'Đơn hàng được tạo',
        timestamp: this.order.createdAt,
        icon: 'plus-circle',
        completed: true
      });

      // Add status history events
      const statusOrder = ['pending', 'confirmed', 'shipped', 'delivered'];
      
      statusOrder.forEach(status => {
        const statusInfo = this.getStatusInfo(status);
        const isCompleted = this.isStatusCompleted(status);
        
        this.statusTimeline.push({
          status: status,
          label: statusInfo.label,
          timestamp: this.getStatusTimestamp(status),
          icon: statusInfo.icon,
          completed: isCompleted,
          isCurrent: this.order.status === status
        });
      });

      // Add cancellation if applicable
      if (this.order.status === 'cancelled') {
        this.statusTimeline.push({
          status: 'cancelled',
          label: 'Đơn hàng bị hủy',
          timestamp: this.order.cancelledAt,
          icon: 'x-circle',
          completed: true
        });
      }
    },

    isStatusCompleted(status) {
      if (!this.order) return false;
      
      const statusOrder = ['pending', 'confirmed', 'shipped', 'delivered'];
      const currentIndex = statusOrder.indexOf(this.order.status);
      const targetIndex = statusOrder.indexOf(status);
      
      return targetIndex < currentIndex || (targetIndex === currentIndex && this.order.status !== 'cancelled');
    },

    getStatusTimestamp(status) {
      if (!this.order) return null;
      
      // Nếu có statusHistory, lấy từ đó
      if (this.order.statusHistory && Array.isArray(this.order.statusHistory)) {
        const history = this.order.statusHistory.find(h => h.status === status);
        return history ? history.timestamp : null;
      }
      
      // Fallback: nếu status hiện tại, dùng updatedAt
      if (this.order.status === status) {
        return this.order.updatedAt;
      }
      
      return null;
    },

    async connectToHub() {
      try {
        // Khởi tạo SignalR connection
        const connection = new signalR.HubConnectionBuilder()
          .withUrl('/order-hub')
          .withAutomaticReconnect()
          .build();

        connection.on('OrderStatusChanged', (orderId, newStatus, timestamp) => {
          if (orderId === this.orderId) {
            console.log('Order status changed:', newStatus);
            this.order.status = newStatus;
            this.order.updatedAt = timestamp;
            this.buildTimeline();
            this.showStatusChangeNotification(newStatus);
          }
        });

        connection.on('OrderShipped', (orderId, trackingNumber) => {
          if (orderId === this.orderId) {
            console.log('Order shipped with tracking:', trackingNumber);
            this.order.trackingNumber = trackingNumber;
            this.buildTimeline();
            this.showNotification('Đơn hàng của bạn đã được giao cho đơn vị vận chuyển');
          }
        });

        connection.on('OrderDelivered', (orderId, deliveryTime) => {
          if (orderId === this.orderId) {
            console.log('Order delivered at:', deliveryTime);
            this.order.deliveredAt = deliveryTime;
            this.buildTimeline();
            this.showNotification('Đơn hàng của bạn đã được giao thành công');
          }
        });

        connection.onreconnected(() => {
          console.log('Reconnected to order hub');
          this.isConnected = true;
        });

        connection.onreconnecting(() => {
          console.log('Reconnecting to order hub...');
          this.isConnected = false;
        });

        await connection.start();
        this.hubConnection = connection;
        this.isConnected = true;

        // Subscribe to order updates
        if (connection.invoke) {
          await connection.invoke('SubscribeToOrder', this.orderId);
        }
      } catch (e) {
        console.error('Error connecting to order hub:', e);
        // Fallback: poll for updates every 30 seconds
        this.startPolling();
      }
    },

    startPolling() {
      setInterval(() => {
        this.loadOrderDetail();
      }, 30000); // Poll every 30 seconds
    },

    showStatusChangeNotification(newStatus) {
      const statusInfo = this.getStatusInfo(newStatus);
      this.showNotification(`Đơn hàng của bạn: ${statusInfo.label}`);
    },

    showNotification(message) {
      // Sử dụng toast notification
      if (typeof showToast !== 'undefined') {
        showToast(message, 'info');
      } else {
        alert(message);
      }
    },

    async cancelOrder() {
      if (!confirm('Bạn chắc chắn muốn hủy đơn hàng này?')) {
        return;
      }

      try {
        const res = await fetch(`/api/orders/${this.orderId}/cancel`, { method: 'POST' });
        if (!res.ok) throw new Error('cancel_failed');
        
        await this.loadOrderDetail();
        alert('Đơn hàng đã được hủy');
      } catch (e) {
        console.error('Cancel order error:', e);
        alert('Không thể hủy đơn hàng');
      }
    },

    async reorderItems() {
      if (!confirm('Thêm tất cả sản phẩm vào giỏ hàng?')) {
        return;
      }

      try {
        const res = await fetch(`/api/orders/${this.orderId}/reorder`, { method: 'POST' });
        if (!res.ok) throw new Error('reorder_failed');
        
        alert('Các sản phẩm đã được thêm vào giỏ hàng');
        window.dispatchEvent(new Event('cart:changed'));
      } catch (e) {
        console.error('Reorder error:', e);
        alert('Không thể tạo lại đơn hàng');
      }
    },

    canCancelOrder() {
      if (!this.order) return false;
      return ['pending', 'confirmed'].includes(this.order.status);
    },

    destroy() {
      // Disconnect from hub
      if (this.hubConnection) {
        this.hubConnection.stop();
      }
    }
  };
}

