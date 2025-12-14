// Alpine component for Checkout Page
// Requirements:
// - Cart stored in cookie, fetched via GET /api/cart
// - Shipping quote via GET /api/shipping/quote?addressId=...&subtotal=...
// - Place order via POST /api/orders/checkout with { addressId, couponCode?, paymentMethod }
// Optional:
// - If you don't have user addresses API yet, prefill selectedAddressId from page or ask user to add in Profile > Address.

function checkoutPage(initial){
  return {
    // Props / initials
    selectedAddressId: initial?.addressId || null,
    paymentMethod: 'COD', // COD|VNPAY|MOMO
    couponCode: '',

    // Data
    addresses: [], // optional: load from API if available
    items: [],
    subtotal: 0,
    shippingFee: 0,
    discount: 0, // for display only; server is source of truth
    total: 0,

    loading: false,
    quoting: false,
    placing: false,
    error: '',

    fmt(v){ try{ return '₫' + Number(v||0).toLocaleString('vi-VN'); }catch{ return v; } },

    async init(){
      // Kiểm tra đăng nhập
      if (typeof authHelper !== 'undefined' && !authHelper.isLoggedIn()) {
        authHelper.requireLogin('/checkout');
        return;
      }
      
      await this.loadCart();
      if(this.selectedAddressId){
        await this.quoteShipping();
      }
      // re-quote when subtotal changes (cart modified elsewhere)
      window.addEventListener('cart:changed', async () => {
        await this.loadCart();
        await this.quoteShipping();
      });
    },

    async loadCart(){
      this.loading = true; this.error = '';
      try{
        const res = await fetch('/api/cart');
        const j = await res.json();
        this.items = (j && j.data) || [];
        this.subtotal = (j && (j.total ?? 0)) || 0;
        this.computeTotal();
      }catch(e){
        this.error = 'cannot_load_cart';
      }finally{
        this.loading = false;
      }
    },

    computeTotal(){
      const sub = Number(this.subtotal||0);
      const ship = Number(this.shippingFee||0);
      const disc = Number(this.discount||0);
      this.total = Math.max(0, sub - disc) + ship;
    },

    async quoteShipping(){
      if(!this.selectedAddressId){ this.shippingFee = 0; this.computeTotal(); return; }
      if(this.subtotal < 0) this.subtotal = 0;
      this.quoting = true; this.error = '';
      try{
        const params = new URLSearchParams();
        params.set('addressId', this.selectedAddressId);
        params.set('subtotal', this.subtotal);
        const res = await fetch(`/api/shipping/quote?${params.toString()}`);
        if(!res.ok) throw new Error('quote_failed');
        const j = await res.json();
        this.shippingFee = (j && j.data && j.data.fee) || 0;
        this.computeTotal();
      }catch(e){
        this.shippingFee = 0; this.computeTotal(); this.error = 'quote_failed';
      }finally{
        this.quoting = false;
      }
    },

    async placeOrder(){
      if(!this.selectedAddressId){ alert((window.I18N && window.I18N.msg_select_address) || 'Please select a shipping address'); return; }
      if((this.items||[]).length === 0){ alert((window.I18N && window.I18N.msg_cart_empty) || 'Cart is empty'); return; }
      this.placing = true; this.error = '';
      try{
        const payload = { addressId: Number(this.selectedAddressId), couponCode: (this.couponCode||'').trim(), paymentMethod: this.paymentMethod };
        const res = await fetch('/api/orders/checkout', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) });
        const j = await res.json();
        if(!res.ok || j.error || j.message){
          throw new Error(j.error || j.message || 'checkout_failed');
        }
        // Server is the source of truth for money
        // Redirect to Orders page (or detail page if available)
        window.location.href = `/Order/MyOrders`;
      }catch(e){
        console.error(e);
        alert('Đặt hàng thất bại, vui lòng thử lại');
      }finally{
        this.placing = false;
      }
    }
  }
}

