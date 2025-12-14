// AlpineJS component for shipping quote (zone-based)
// Usage: <div x-data="shippingQuote({ addressId: 123, subtotal: 150000 })" x-init="init()">
//          <span x-text="fmt(fee)"></span>
//        </div>
// If you don't have addressId, pass zoneId instead: shippingQuote({ zoneId: 1, subtotal: 150000 })

function shippingQuote(initial){
  return {
    addressId: initial?.addressId || null,
    zoneId: initial?.zoneId || null,
    subtotal: initial?.subtotal || 0,
    fee: 0,
    loading: false,
    error: '',
    init(){
      this.$watch('addressId', () => this.quote());
      this.$watch('zoneId', () => this.quote());
      this.$watch('subtotal', () => this.quote());
      this.quote();
    },
    fmt(v){ try{ return '₫' + Number(v||0).toLocaleString('vi-VN'); }catch{ return v; } },
    async quote(){
      this.error='';
      if(!(this.addressId || this.zoneId)) { this.fee = 0; return; }
      if(this.subtotal < 0) this.subtotal = 0;
      this.loading = true;
      try{
        const params = new URLSearchParams();
        if(this.addressId) params.set('addressId', this.addressId);
        if(this.zoneId) params.set('zoneId', this.zoneId);
        params.set('subtotal', this.subtotal);
        const res = await fetch(`/api/shipping/quote?${params.toString()}`);
        if(!res.ok){ throw new Error('quote_failed'); }
        const j = await res.json();
        this.fee = (j && j.data && j.data.fee) || 0;
        // emit global event
        window.dispatchEvent(new CustomEvent('shipping:quoted', { detail: { fee: this.fee, zone: (j && j.data && j.data.zone)||null } }));
      }catch(e){
        this.error = 'quote_failed';
        this.fee = 0;
      }finally{
        this.loading = false;
      }
    }
  }
}

