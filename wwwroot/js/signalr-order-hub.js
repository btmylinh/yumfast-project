// SignalR Order Hub Connection
// Quản lý kết nối realtime với server để nhận updates về đơn hàng

const orderHub = {
  connection: null,
  isConnected: false,
  reconnectAttempts: 0,
  maxReconnectAttempts: 5,
  reconnectDelay: 3000,
  
  /**
   * Khởi tạo kết nối SignalR
   * @returns {Promise<void>}
   */
  async init() {
    if (this.connection) {
      console.warn('Order hub already initialized');
      return;
    }

    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl('/order-hub', {
          skipNegotiation: true,
          transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect([0, 0, 1000, 3000, 5000, 10000])
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Setup event handlers
      this.setupEventHandlers();

      // Start connection
      await this.connection.start();
      this.isConnected = true;
      this.reconnectAttempts = 0;
      console.log('Order hub connected');
    } catch (err) {
      console.error('Error connecting to order hub:', err);
      this.scheduleReconnect();
    }
  },

  /**
   * Setup các event handlers
   */
  setupEventHandlers() {
    if (!this.connection) return;

    // Khi connection bị mất
    this.connection.onclose(async () => {
      this.isConnected = false;
      console.log('Order hub disconnected');
      this.scheduleReconnect();
    });

    // Khi reconnecting
    this.connection.onreconnecting(() => {
      this.isConnected = false;
      console.log('Order hub reconnecting...');
    });

    // Khi reconnected
    this.connection.onreconnected(() => {
      this.isConnected = true;
      this.reconnectAttempts = 0;
      console.log('Order hub reconnected');
    });

    // Nhận thông báo cập nhật trạng thái đơn hàng
    this.connection.on('OrderStatusChanged', (orderId, newStatus, timestamp) => {
      console.log(`Order ${orderId} status changed to ${newStatus}`);
      this.dispatchEvent('order:statusChanged', { orderId, newStatus, timestamp });
    });

    // Nhận thông báo đơn hàng được xác nhận
    this.connection.on('OrderConfirmed', (orderId, confirmTime) => {
      console.log(`Order ${orderId} confirmed at ${confirmTime}`);
      this.dispatchEvent('order:confirmed', { orderId, confirmTime });
    });

    // Nhận thông báo đơn hàng được giao cho vận chuyển
    this.connection.on('OrderShipped', (orderId, trackingNumber, shippedTime) => {
      console.log(`Order ${orderId} shipped with tracking ${trackingNumber}`);
      this.dispatchEvent('order:shipped', { orderId, trackingNumber, shippedTime });
    });

    // Nhận thông báo đơn hàng đã giao
    this.connection.on('OrderDelivered', (orderId, deliveryTime) => {
      console.log(`Order ${orderId} delivered at ${deliveryTime}`);
      this.dispatchEvent('order:delivered', { orderId, deliveryTime });
    });

    // Nhận thông báo đơn hàng bị hủy
    this.connection.on('OrderCancelled', (orderId, reason, cancelTime) => {
      console.log(`Order ${orderId} cancelled: ${reason}`);
      this.dispatchEvent('order:cancelled', { orderId, reason, cancelTime });
    });

    // Nhận thông báo lỗi
    this.connection.on('Error', (message) => {
      console.error('Order hub error:', message);
      this.dispatchEvent('order:error', { message });
    });

    // Nhận thông báo chung
    this.connection.on('Notification', (message) => {
      console.log('Order hub notification:', message);
      this.dispatchEvent('order:notification', { message });
    });
  },

  /**
   * Lên lịch reconnect
   */
  scheduleReconnect() {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.error('Max reconnect attempts reached');
      return;
    }

    this.reconnectAttempts++;
    const delay = this.reconnectDelay * this.reconnectAttempts;
    console.log(`Scheduling reconnect in ${delay}ms (attempt ${this.reconnectAttempts})`);

    setTimeout(() => {
      this.init();
    }, delay);
  },

  /**
   * Phát sự kiện tới các listener
   * @param {string} eventName - Tên sự kiện
   * @param {Object} data - Dữ liệu sự kiện
   */
  dispatchEvent(eventName, data) {
    const event = new CustomEvent(`order-hub:${eventName}`, { detail: data });
    window.dispatchEvent(event);
  },

  /**
   * Subscribe tới updates của một đơn hàng
   * @param {number} orderId - ID của đơn hàng
   */
  async subscribeToOrder(orderId) {
    if (!this.isConnected || !this.connection) {
      console.warn('Order hub not connected');
      return;
    }

    try {
      await this.connection.invoke('SubscribeToOrder', orderId);
      console.log(`Subscribed to order ${orderId}`);
    } catch (err) {
      console.error(`Error subscribing to order ${orderId}:`, err);
    }
  },

  /**
   * Unsubscribe khỏi updates của một đơn hàng
   * @param {number} orderId - ID của đơn hàng
   */
  async unsubscribeFromOrder(orderId) {
    if (!this.isConnected || !this.connection) {
      console.warn('Order hub not connected');
      return;
    }

    try {
      await this.connection.invoke('UnsubscribeFromOrder', orderId);
      console.log(`Unsubscribed from order ${orderId}`);
    } catch (err) {
      console.error(`Error unsubscribing from order ${orderId}:`, err);
    }
  },

  /**
   * Lấy trạng thái kết nối
   * @returns {boolean}
   */
  getConnectionStatus() {
    return this.isConnected;
  },

  /**
   * Disconnect từ hub
   */
  async disconnect() {
    if (this.connection) {
      try {
        await this.connection.stop();
        this.isConnected = false;
        console.log('Order hub disconnected');
      } catch (err) {
        console.error('Error disconnecting from order hub:', err);
      }
    }
  }
};

// Auto-initialize khi DOM ready (nếu user đã đăng nhập)
document.addEventListener('DOMContentLoaded', async () => {
  if (typeof authHelper !== 'undefined' && authHelper.isLoggedIn()) {
    // Delay initialization để đảm bảo SignalR library đã load
    setTimeout(() => {
      if (typeof signalR !== 'undefined') {
        orderHub.init().catch(err => console.error('Failed to initialize order hub:', err));
      }
    }, 100);
  }
});

// Cleanup khi page unload
window.addEventListener('beforeunload', () => {
  orderHub.disconnect();
});

