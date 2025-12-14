// Product Sections - Alpine.js Integration
document.addEventListener('DOMContentLoaded', async () => {
  // Format currency
  const formatPrice = (price) => '₫' + Number(price || 0).toLocaleString('vi-VN');
  const T = (k, d) => (window.I18N && window.I18N[k]) || d;

  // Load New Products
  const loadNewProducts = async () => {
    const container = document.getElementById('newProductsContainer');
    if (!container) return;

    try {
      const response = await fetch('/api/products/new-products?limit=12');
      const json = await response.json();
      const products = json.data || [];

      container.innerHTML = '';
      products.forEach(product => {
        const col = document.createElement('div');
        col.className = 'col-6 col-md-4 col-lg-3';
        col.innerHTML = `
          <div class="card card-product mb-lg-4">
            <div class="card-body">
              <div class="text-center position-relative">
                <div class="position-absolute top-0 start-0">
                  <span class="badge bg-danger">${T('newBadge','New')}</span>
                </div>
                <a href="/product/detail/${product.id}">
                  <img src="${product.image}" alt="${product.name}" class="mb-3 img-fluid" />
                </a>
                <div class="card-product-action">
                  <a href="#!" class="btn-action" data-bs-toggle="modal" data-bs-target="#quickViewModal">
                    <i class="bi bi-eye" data-bs-toggle="tooltip" data-bs-html="true" title="${T('quickView','Quick View')}"></i>
                  </a>
                  <a href="/shop/wishlist" class="btn-action" data-bs-toggle="tooltip" data-bs-html="true" title="${T('wishlist','Wishlist')}">
                    <i class="bi bi-heart"></i>
                  </a>
                  <a href="#!" class="btn-action" data-bs-toggle="tooltip" data-bs-html="true" title="${T('compare','Compare')}">
                    <i class="bi bi-arrow-left-right"></i>
                  </a>
                </div>
              </div>
              <div class="text-small mb-1">
                <a href="#!" class="text-decoration-none text-muted"><small>${product.category}</small></a>
              </div>
              <h2 class="fs-6"><a href="/product/detail/${product.id}" class="text-inherit text-decoration-none">${product.name}</a></h2>
              <div>
                <small class="text-warning">
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-half"></i>
                </small>
                <span class="text-muted small">4.5(149)</span>
              </div>
              <div class="d-flex justify-content-between align-items-center mt-3">
                <div>
                  <span class="text-dark">${formatPrice(product.price)}</span>
                </div>
                <div>
                  <a href="#!" class="btn btn-primary btn-sm btn-add-cart" data-id="${product.id}">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="feather feather-plus">
                      <line x1="12" y1="5" x2="12" y2="19"></line>
                      <line x1="5" y1="12" x2="19" y2="12"></line>
                    </svg>
                    ${T('addToCart','Add to Cart')}
                  </a>
                </div>
              </div>
            </div>
          </div>
        `;
        container.appendChild(col);
      });
    } catch (error) {
      console.error('Error loading new products:', error);
    }
  };

  // Load Best Selling Products
  const loadBestSelling = async () => {
    const container = document.getElementById('bestSellingContainer');
    if (!container) return;

    try {
      const response = await fetch('/api/products/best-selling?limit=12');
      const json = await response.json();
      const products = json.data || [];

      container.innerHTML = '';
      products.forEach(product => {
        const col = document.createElement('div');
        col.className = 'col-6 col-md-4 col-lg-3';
        col.innerHTML = `
          <div class="card card-product mb-lg-4">
            <div class="card-body">
              <div class="text-center position-relative">
                <div class="position-absolute top-0 start-0">
                  <span class="badge bg-danger">${T('saleBadge','Sale')}</span>
                </div>
                <a href="/product/detail/${product.id}">
                  <img src="${product.image}" alt="${product.name}" class="mb-3 img-fluid" />
                </a>
                <div class="card-product-action">
                  <a href="#!" class="btn-action" data-bs-toggle="modal" data-bs-target="#quickViewModal">
                    <i class="bi bi-eye" data-bs-toggle="tooltip" data-bs-html="true" title="Quick View"></i>
                  </a>
                  <a href="/shop/wishlist" class="btn-action" data-bs-toggle="tooltip" data-bs-html="true" title="${T('wishlist','Wishlist')}">
                    <i class="bi bi-heart"></i>
                  </a>
                  <a href="#!" class="btn-action" data-bs-toggle="tooltip" data-bs-html="true" title="${T('compare','Compare')}">
                    <i class="bi bi-arrow-left-right"></i>
                  </a>
                </div>
              </div>
              <div class="text-small mb-1">
                <a href="#!" class="text-decoration-none text-muted"><small>${product.category}</small></a>
              </div>
              <h2 class="fs-6"><a href="/product/detail/${product.id}" class="text-inherit text-decoration-none">${product.name}</a></h2>
              <div>
                <small class="text-warning">
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-half"></i>
                </small>
                <span class="text-muted small">4.5(149)</span>
              </div>
              <div class="d-flex justify-content-between align-items-center mt-3">
                <div>
                  <span class="text-dark">${formatPrice(product.price)}</span>
                </div>
                <div>
                  <a href="#" class="btn btn-primary btn-sm btn-add-cart" data-id="${product.id}">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="feather feather-plus">
                      <line x1="12" y1="5" x2="12" y2="19"></line>
                      <line x1="5" y1="12" x2="19" y2="12"></line>
                    </svg>
                    ${T('addToCart','Add to Cart')}
                  </a>
                </div>
              </div>
            </div>
          </div>
        `;
        container.appendChild(col);
      });
    } catch (error) {
      console.error('Error loading best selling products:', error);
    }
  };

  // Load Deal of the Day
  const loadDealOfDay = async () => {
    const container = document.getElementById('dealOfDayContainer');
    if (!container) return;

    try {
      const response = await fetch('/api/products/deal-of-day?limit=1');
      const json = await response.json();
      const products = json.data || [];

      if (products.length === 0) return;

      const product = products[0];
      container.innerHTML = `
        <div class="card border border-danger p-6">
          <div class="row">
            <div class="col-lg-5 text-center">
              <a href="/product/detail/${product.id}">
                <img src="${product.image}" alt="${product.name}" class="img-fluid" style="max-height: 300px; object-fit: cover;" />
              </a>
            </div>
            <div class="col-lg-7 text-center text-lg-start">
              <div class="mb-3">
                <small class="text-warning">
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-fill"></i>
                  <i class="bi bi-star-half"></i>
                </small>
                <span><small>4.5</small></span>
              </div>
              <h2 class="fs-4"><a href="/product/detail/${product.id}" class="text-inherit text-decoration-none">${product.name}</a></h2>
              <div class="d-flex justify-content-center align-items-center justify-content-lg-between mt-3">
                <div>
                  <span class="text-dark fs-5 fw-bold">${formatPrice(product.price)}</span>
                </div>
              </div>
              <div class="mt-2">
                <a href="javascript:void(0)" class="btn btn-primary btn-add-cart" data-id="${product.id}">
                  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="feather feather-plus">
                    <line x1="12" y1="5" x2="12" y2="19"></line>
                    <line x1="5" y1="12" x2="19" y2="12"></line>
                  </svg>
                  ${T('addToCart','Add to Cart')}
                </a>
              </div>
              <div class="mt-6 mb-6">
                <div class="d-flex justify-content-between mb-2">
                  <span>
                    ${T('alreadySold','Already Sold')}:
                    <span class="text-dark fs-6 fw-bold">45</span>
                  </span>
                  <span>
                    ${T('available','Available')}:
                    <span class="text-dark fs-6 fw-bold">25</span>
                  </span>
                </div>
                <div class="progress bg-light-danger" role="progressbar" aria-label="Example 1px high" aria-valuenow="85" aria-valuemin="0" aria-valuemax="100" style="height: 5px">
                  <div class="progress-bar bg-danger" style="width: 85%"></div>
                </div>
              </div>
              <p class="fw-bold text-dark mb-0">${T('dealHurry','Hurry up offer ends soon')}</p>
              <div class="d-flex justify-content-center justify-content-lg-start text-center mt-1">
                <div class="deals-countdown" data-countdown="2024/10/10 00:00:00"></div>
              </div>
            </div>
          </div>
        </div>
      `;
    } catch (error) {
      console.error('Error loading deal of day:', error);
    }
  };

  // Load all sections
  await Promise.all([
    loadNewProducts(),
    loadBestSelling(),
    loadDealOfDay()
  ]);

  // Global Add-to-Cart handler (event delegation)
  document.addEventListener('click', async (e) => {
    const btn = e.target.closest('.btn-add-cart');
    if (!btn) return;
    
    // Kiểm tra đăng nhập
    if (typeof authHelper !== 'undefined' && !authHelper.isLoggedIn()) {
      await authHelper.showLoginModal('Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng');
      return;
    }
    
    const id = parseInt(btn.getAttribute('data-id'));
    if (!id) return;
    
    // Disable button khi đang xử lý
    const originalText = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang thêm...';
    
    try {
      const res = await fetch('/api/cart/add', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ productId: id, quantity: 1 })
      });
      
      if (res.ok) {
        // Hiển thị thông báo thành công
        const successMsg = (window.I18N && window.I18N.added_to_cart_success) || 'Đã thêm vào giỏ hàng';
        showToast(successMsg, 'success');
        window.dispatchEvent(new Event('cart:changed'));
      } else if (res.status === 401) {
        // Unauthorized - redirect to login
        await authHelper.showLoginModal('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.');
      } else {
        const errorMsg = (window.I18N && window.I18N.generic_error) || 'Lỗi khi thêm vào giỏ hàng';
        showToast(errorMsg, 'error');
      }
    } catch (err) {
      console.error('Add to cart error:', err);
      const errorMsg = (window.I18N && window.I18N.server_connection_error) || 'Lỗi kết nối';
      showToast(errorMsg, 'error');
    } finally {
      // Restore button state
      btn.disabled = false;
      btn.innerHTML = originalText;
    }
  });
  
  // Helper function to show toast notifications
  function showToast(message, type = 'info') {
    const toastId = 'toast-' + Date.now();
    const bgClass = {
      'success': 'bg-success',
      'error': 'bg-danger',
      'warning': 'bg-warning',
      'info': 'bg-info'
    }[type] || 'bg-info';
    
    const toast = document.createElement('div');
    toast.id = toastId;
    toast.className = `toast align-items-center text-white ${bgClass} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    toast.innerHTML = `
      <div class="d-flex">
        <div class="toast-body">${message}</div>
        <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
      </div>
    `;
    
    // Create container if not exists
    let container = document.getElementById('toast-container');
    if (!container) {
      container = document.createElement('div');
      container.id = 'toast-container';
      container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
      document.body.appendChild(container);
    }
    
    container.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();
    
    // Auto remove after hide
    toast.addEventListener('hidden.bs.toast', () => {
      toast.remove();
    });
  }
});

