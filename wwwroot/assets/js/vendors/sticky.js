// Safe StickySidebar init - only when element exists and after DOM ready
// Prevents: Cannot read properties of null (reading 'parentElement')

document.addEventListener('DOMContentLoaded', function () {
  var el = document.querySelector('#sidebar');
  if (!el) return; // no sidebar on this page
  try {
    // Ensure container/inner selectors match your markup
    window.stickySidebar = new StickySidebar('#sidebar', {
      topSpacing: 20,
      bottomSpacing: 20,
      containerSelector: '.product-content',
      innerWrapperSelector: '.sidebar__inner',
      resizeSensor: true,
      minWidth: 330
    });
  } catch (e) {
    console.warn('StickySidebar init failed:', e);
  }
});
