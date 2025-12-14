// Alpine component for Order History Page
// Usage: <div x-data="orderHistory()" x-init="init()">

function orderHistory() {
  return {
    orders: [],
    filteredOrders: [],
    loading: false,
    error: '',
    
    // Filter options
    filterStatus: 'all', // all, pending, confirmed, shipped, delivered, cancelled
    filterDateFrom: '',
    filterDateTo: '',
    searchText: '',
    
    // Pagination
    currentPage: 1,
    pageSize: 10,
    totalPages: 1,
    
    // Status mapping
    statusMap: {
      'pending': { label: 'Chờ xác nhận', badge: 'warning' },
      'confirmed': { label: 'Đã xác nhận', badge: 'info' },
      'shipped': { label: 'Đang giao', badge: 'primary' },
      'delivered': { label: 'Đã giao', badge: 'success' },
      'cancelled': { label: 'Đã hủy', badge: 'danger' }
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

    getStatusBadge(status) {
      const info = this.statusMap[status] || { label: status, badge: 'secondary' };
      return info;
    },

    async init() {
      // Kiểm tra đăng nhập
      if (typeof authHelper !== 'undefined' && !authHelper.isLoggedIn()) {
        authHelper.requireLogin('/user/orders');
        return;
      }
      
      await this.loadOrders();
      
      // Watch filter changes
      this.$watch('filterStatus', () => this.applyFilters());
      this.$watch('filterDateFrom', () => this.applyFilters());
      this.$watch('filterDateTo', () => this.applyFilters());
      this.$watch('searchText', () => this.applyFilters());
    },

    async loadOrders() {
      this.loading = true;
      this.error = '';
      try {
        const res = await fetch('/api/orders');
        if (!res.ok) throw new Error('load_failed');
        
        const json = await res.json();
        this.orders = (json && json.data) || [];
        this.applyFilters();
      } catch (e) {
        console.error('Load orders error:', e);
        this.error = 'Không thể tải danh sách đơn hàng';
      } finally {
        this.loading = false;
      }
    },

    applyFilters() {
      let filtered = [...this.orders];

      // Filter by status
      if (this.filterStatus !== 'all') {
        filtered = filtered.filter(o => o.status === this.filterStatus);
      }

      // Filter by date range
      if (this.filterDateFrom) {
        const fromDate = new Date(this.filterDateFrom);
        filtered = filtered.filter(o => new Date(o.createdAt) >= fromDate);
      }
      if (this.filterDateTo) {
        const toDate = new Date(this.filterDateTo);
        toDate.setHours(23, 59, 59, 999);
        filtered = filtered.filter(o => new Date(o.createdAt) <= toDate);
      }

      // Filter by search text (tìm kiếm theo order ID hoặc địa chỉ)
      if (this.searchText) {
        const search = this.searchText.toLowerCase();
        filtered = filtered.filter(o => 
          (o.id && o.id.toString().includes(search)) ||
          (o.shippingAddress && o.shippingAddress.toLowerCase().includes(search))
        );
      }

      // Sort by date (newest first)
      filtered.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));

      // Pagination
      this.totalPages = Math.ceil(filtered.length / this.pageSize);
      const startIdx = (this.currentPage - 1) * this.pageSize;
      this.filteredOrders = filtered.slice(startIdx, startIdx + this.pageSize);
    },

    resetFilters() {
      this.filterStatus = 'all';
      this.filterDateFrom = '';
      this.filterDateTo = '';
      this.searchText = '';
      this.currentPage = 1;
      this.applyFilters();
    },

    goToPage(page) {
      if (page >= 1 && page <= this.totalPages) {
        this.currentPage = page;
        this.applyFilters();
        // Scroll to top
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    },

    viewOrderDetail(orderId) {
      window.location.href = `/Order/Tracking/${orderId}`;
    },

    async cancelOrder(orderId) {
      if (!confirm('Bạn chắc chắn muốn hủy đơn hàng này?')) {
        return;
      }

      try {
        const res = await fetch(`/api/orders/${orderId}/cancel`, { method: 'POST' });
        if (!res.ok) throw new Error('cancel_failed');
        
        // Reload orders
        await this.loadOrders();
        alert('Đơn hàng đã được hủy');
      } catch (e) {
        console.error('Cancel order error:', e);
        alert('Không thể hủy đơn hàng');
      }
    },

    async reorderItems(orderId) {
      try {
        const res = await fetch(`/api/orders/${orderId}/reorder`, { method: 'POST' });
        if (!res.ok) throw new Error('reorder_failed');
        
        alert('Các sản phẩm đã được thêm vào giỏ hàng');
        window.dispatchEvent(new Event('cart:changed'));
      } catch (e) {
        console.error('Reorder error:', e);
        alert('Không thể tạo lại đơn hàng');
      }
    }
  };
}

