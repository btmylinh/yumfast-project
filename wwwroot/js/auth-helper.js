// Auth Helper - Kiểm tra đăng nhập và quản lý authentication
// Usage: authHelper.checkLogin() -> boolean
//        authHelper.requireLogin() -> redirect to login if not authenticated

const authHelper = {
  /**
   * Kiểm tra xem người dùng đã đăng nhập chưa
   * @returns {boolean} true nếu đã đăng nhập, false nếu chưa
   */
  isLoggedIn() {
    // Cách 1: Kiểm tra từ cookie
    const cookies = document.cookie.split(';');
    for (let cookie of cookies) {
      const [name, value] = cookie.trim().split('=');
      if (name === 'auth_token' && value) {
        return true;
      }
    }
    
    // Cách 2: Kiểm tra từ localStorage
    if (localStorage.getItem('auth_token') || localStorage.getItem('token')) {
      return true;
    }
    
    // Cách 3: Kiểm tra từ HTML attribute (nếu server render)
    const userElement = document.querySelector('[data-user-id]');
    if (userElement && userElement.getAttribute('data-user-id')) {
      return true;
    }
    
    return false;
  },

  /**
   * Lấy thông tin người dùng hiện tại
   * @returns {Object|null} Thông tin user hoặc null
   */
  getCurrentUser() {
    try {
      const userJson = localStorage.getItem('current_user');
      if (userJson) {
        return JSON.parse(userJson);
      }
    } catch (e) {
      console.error('Error parsing user data:', e);
    }
    return null;
  },

  /**
   * Yêu cầu đăng nhập - redirect nếu chưa đăng nhập
   * @param {string} returnUrl - URL để quay lại sau khi đăng nhập (mặc định: trang hiện tại)
   * @returns {boolean} true nếu đã đăng nhập, false nếu redirect
   */
  requireLogin(returnUrl = null) {
    if (this.isLoggedIn()) {
      return true;
    }
    
    // Lưu URL hiện tại để quay lại sau khi đăng nhập
    const url = returnUrl || window.location.href;
    sessionStorage.setItem('return_url', url);
    
    // Redirect đến trang đăng nhập
    const loginUrl = '/auth/SignIn';
    window.location.href = loginUrl + (url ? ('?returnUrl=' + encodeURIComponent(url)) : '');
    return false;
  },

  /**
   * Hiển thị modal yêu cầu đăng nhập
   * @param {string} message - Thông báo tùy chỉnh
   * @returns {Promise} Promise resolve khi user đóng modal
   */
  showLoginModal(message = null) {
    return new Promise((resolve) => {
      const modal = document.createElement('div');
      modal.className = 'modal fade';
      modal.id = 'loginRequiredModal';
      modal.setAttribute('tabindex', '-1');
      modal.innerHTML = `
        <div class="modal-dialog modal-dialog-centered">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Yêu cầu đăng nhập</h5>
              <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
              <p>${message || 'Vui lòng đăng nhập để tiếp tục.'}</p>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Hủy</button>
              <a href="/auth/SignIn" class="btn btn-primary">Đăng nhập</a>
            </div>
          </div>
        </div>
      `;
      
      document.body.appendChild(modal);
      const bsModal = new bootstrap.Modal(modal);
      
      modal.addEventListener('hidden.bs.modal', () => {
        modal.remove();
        resolve();
      });
      
      bsModal.show();
    });
  },

  /**
   * Thực hiện hành động chỉ khi đã đăng nhập
   * @param {Function} callback - Hàm callback để thực hiện
   * @param {string} message - Thông báo nếu chưa đăng nhập
   * @returns {Promise}
   */
  async requireLoginFor(callback, message = null) {
    if (!this.isLoggedIn()) {
      await this.showLoginModal(message);
      return;
    }
    return callback();
  }
};

