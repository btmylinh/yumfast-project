document.addEventListener('DOMContentLoaded', async () => {
  const idMatch = location.pathname.match(/product\/(\d+)/i);
  const id = idMatch ? idMatch[1] : null;
  if (!id) return;
  const res = await fetch('/api/products/' + id);
  if (!res.ok) return;
  const json = await res.json();
  const p = json.data;
  const nameEl = document.getElementById('productName'); if (nameEl) nameEl.textContent = p.name;
  const cat = document.getElementById('productCategory'); if (cat) { cat.textContent = p.category; cat.href = '/shop/category?category=' + encodeURIComponent(p.category); }
  const priceEl = document.getElementById('productPrice'); let extra = 0; const updatePrice = () => { if (priceEl) priceEl.textContent = '₫' + (p.price + extra).toLocaleString('vi-VN'); }; updatePrice();
  const descEl = document.getElementById('productDesc'); if (descEl) descEl.textContent = p.description || '';

  const gallery = document.getElementById('productGallery'); const thumbs = document.getElementById('productThumbnails');
  if (gallery && thumbs) { gallery.innerHTML = ''; thumbs.innerHTML = ''; (p.images || []).forEach((src) => { const g = document.createElement('div'); g.className = 'zoom'; g.style.backgroundImage = `url(${src})`; g.innerHTML = `<img src="${src}" alt="${p.name}" onerror="this.src='/assets/images/docs/placeholder-img.jpg'" />`; gallery.appendChild(g); const col = document.createElement('div'); col.className = 'col-3'; col.innerHTML = `<div class='thumbnails-img'><img src='${src}' alt='${p.name}' onerror="this.src='/assets/images/docs/placeholder-img.jpg'" /></div>`; thumbs.appendChild(col); }); }

  const optionsByType = {}; (p.options || []).forEach(o => { const t = o.type || 'Options'; (optionsByType[t] ||= []).push(o); });
  const optContainer = document.getElementById('productOptions');
  if (optContainer) { Object.entries(optionsByType).forEach(([type, arr]) => { const group = document.createElement('div'); group.className = 'mb-2'; group.innerHTML = `<div class='fw-semibold mb-1'>${type}</div>`; const row = document.createElement('div'); arr.forEach(o => { const btn = document.createElement('button'); btn.type = 'button'; btn.className = 'btn btn-outline-secondary me-2 mb-2'; btn.textContent = `${o.name}${o.price>0?(' (+'+o.price+'₫)'):''}`; btn.dataset.type = type; btn.dataset.price = o.price; btn.dataset.name = o.name; btn.addEventListener('click', () => { row.querySelectorAll('button').forEach(s => s.classList.remove('active')); btn.classList.add('active'); extra = Array.from(optContainer.querySelectorAll('button.active')).reduce((sum,b)=> sum + parseInt(b.dataset.price||'0'), 0); updatePrice(); }); row.appendChild(btn); }); group.appendChild(row); optContainer.appendChild(group); }); }
});
