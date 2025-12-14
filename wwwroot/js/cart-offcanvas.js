// Alpine component for Cart Offcanvas
// Usage example in _Layout.cshtml offcanvas body:
// <div x-data="cartPanel()" x-init="init()">
//   ... render items ...
// </div>

function cartPanel() {
  return {
    items: [],
    total: 0,
    loading: false,
    error: '',

    init() {
      this.load();
      // refresh when global event fired
      window.addEventListener('cart:changed', () => this.load());
    },

    fmt(v) { try { return '₫' + Number(v || 0).toLocaleString('vi-VN'); } catch { return v; } },

    async load() {
      this.loading = true; this.error = '';
      try {
        const res = await fetch('/api/cart');
        const j = await res.json();
        this.items = (j && j.data) || [];
        this.total = (j && (j.total ?? 0)) || 0;
      } catch (e) {
        this.error = 'load_failed';
      } finally {
        this.loading = false;
      }
    },

    async updateQty(item, qty) {
      qty = Math.max(0, Math.min(99, parseInt(qty || 0)));
      try {
        const body = [{ productId: item.id, optionsKey: item.key || '', quantity: qty }];
        const res = await fetch('/api/cart/update', {
          method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body)
        });
        if (!res.ok) throw new Error('update_failed');
        await this.load();
        window.dispatchEvent(new Event('cart:changed'));
      } catch (e) {
        alert('Cập nhật giỏ hàng thất bại');
      }
    },

    async remove(item) {
      try {
        const url = new URL('/api/cart/remove', window.location.origin);
        url.searchParams.set('productId', item.id);
        if (item.key) url.searchParams.set('optionsKey', item.key);
        const res = await fetch(url, { method: 'DELETE' });
        if (!res.ok) throw new Error('remove_failed');
        await this.load();
        window.dispatchEvent(new Event('cart:changed'));
      } catch (e) {
        alert('Xoá sản phẩm thất bại');
      }
    },

    async clear() {
      try {
        const res = await fetch('/api/cart/clear', { method: 'POST' });
        if (!res.ok) throw new Error('clear_failed');
        await this.load();
        window.dispatchEvent(new Event('cart:changed'));
      } catch (e) {
        alert('Xoá giỏ hàng thất bại');
      }
    }
  }
}

