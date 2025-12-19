# Order Tracking Card - UX Specification & Design Guide

## 📋 Tổng quan

Order Tracking Card là component hiển thị tiến trình đơn hàng theo thời gian thực, được thiết kế theo nguyên tắc **mobile-first**, ưu tiên trải nghiệm trên điện thoại.

---

## 🎨 Thiết kế UI

### 1. Cấu trúc Card

```
┌─────────────────────────────────────┐
│  [Icon] Trạng thái hiện tại        │ ← Header (Status)
│       Mã đơn: #ABC123              │
├─────────────────────────────────────┤
│  ●━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━● │ ← Timeline (5 bước)
│  Đã đặt → Chuẩn bị → Tài xế → ... │
├─────────────────────────────────────┤
│  [Thông tin động theo giai đoạn]   │ ← Dynamic Info
│  - Nhà hàng / Tài xế               │
│  - Thời gian dự kiến               │
├─────────────────────────────────────┤
│  [Button] [Button]                 │ ← CTA Actions
└─────────────────────────────────────┘
```

### 2. Kích thước & Spacing

- **Card width**: 100% trên mobile, max-width 600px trên desktop
- **Border radius**: 12px (bo góc mềm mại)
- **Padding**: 
  - Header: `pt-3 pb-2 px-3`
  - Body: `px-3 py-3`
- **Margin**: `mb-3` (khoảng cách với content bên dưới)
- **Shadow**: `shadow-sm` (shadow nhẹ, không quá nổi)

### 3. Màu sắc theo trạng thái

| Trạng thái | Màu chính | Icon | Ý nghĩa |
|------------|-----------|------|---------|
| **Pending** (0) | Warning (Vàng) | `bi-clock-history` | Đang chờ xác nhận |
| **Confirmed** (1) | Info (Xanh dương nhạt) | `bi-cup-hot` | Đã xác nhận |
| **Preparing** (3) | Info (Xanh dương nhạt) | `bi-cup-hot` | Nhà hàng chuẩn bị |
| **DriverAssigned** (2) | Primary (Xanh dương) | `bi-person-check` | Tài xế nhận đơn |
| **Delivering** (4) | Primary (Xanh dương) | `bi-truck` | Đang giao hàng |
| **Completed** (5) | Success (Xanh lá) | `bi-check-circle` | Hoàn thành |
| **Cancelled** (6) | Danger (Đỏ) | `bi-x-circle` | Đã hủy |
| **Failed** (7) | Danger (Đỏ) | `bi-exclamation-triangle` | Thất bại |

### 4. Timeline (Progress Bar)

#### 5 Bước cố định:

1. **Đã đặt hàng** (Step 1)
   - Icon: `bi-check-circle-fill` (khi completed) hoặc `bi-circle` (chưa)
   - Hiển thị thời gian đặt hàng

2. **Chuẩn bị món** (Step 2)
   - Icon: `bi-check-circle-fill` (khi completed) hoặc `bi-circle` (chưa)
   - Hiển thị thời gian xác nhận

3. **Tài xế nhận đơn** (Step 3)
   - Icon: `bi-check-circle-fill` (khi completed) hoặc `bi-circle` (chưa)
   - Hiển thị thời gian tài xế nhận

4. **Đang giao hàng** (Step 4)
   - Icon: `bi-check-circle-fill` (khi completed) hoặc `bi-circle` (chưa)
   - Hiển thị thời gian bắt đầu giao

5. **Hoàn thành** (Step 5)
   - Icon: `bi-check-circle-fill` (khi completed) hoặc `bi-circle` (chưa)
   - Hiển thị thời gian hoàn thành

#### Visual States:

- **Completed Step**: 
  - Background: `#198754` (xanh lá)
  - Border: `#198754`
  - Icon: màu trắng
  - Text: màu xanh lá, font-weight: 600

- **Active Step**:
  - Background: `#0d6efd` (xanh dương)
  - Border: `#0d6efd`
  - Icon: màu trắng
  - Animation: `pulse` (scale 1 → 1.1 → 1, lặp lại)
  - Text: màu xanh dương, font-weight: 600

- **Pending Step**:
  - Background: `#ffffff`
  - Border: `#e9ecef` (xám nhạt)
  - Icon: màu xám
  - Text: màu xám

#### Progress Line:

- Đường nối giữa các bước
- Màu xanh lá cho phần đã hoàn thành
- Màu xám cho phần chưa hoàn thành
- Transition: `0.5s ease` khi cập nhật

---

## 🔄 Luồng hành vi theo từng trạng thái

### Trạng thái 0: Pending (Chờ xác nhận)

**Header:**
- Text: "Đang chờ xác nhận"
- Icon: `bi-clock-history`
- Màu: Warning (vàng)

**Timeline:**
- Step 1: Active (có animation pulse)
- Step 2-5: Pending

**Dynamic Info:**
- Alert warning: "Đơn hàng đang chờ nhà hàng xác nhận"

**CTA:**
- Button: "Hủy đơn hàng" (màu đỏ outline)
- Action: Hiển thị confirm dialog → Gọi API `/api/orders/{id}/cancel`

---

### Trạng thái 1 & 3: Confirmed / Preparing (Nhà hàng chuẩn bị)

**Header:**
- Text: "Nhà hàng đang chuẩn bị món"
- Icon: `bi-cup-hot`
- Màu: Info (xanh dương nhạt)

**Timeline:**
- Step 1: Completed
- Step 2: Active (có animation pulse)
- Step 3-5: Pending

**Dynamic Info:**
- Hiển thị:
  - Tên nhà hàng (bên trái)
  - Thời gian dự kiến giao hàng (bên phải, màu xanh dương)
- Background: `bg-light`, padding: `p-2`, border-radius: `rounded`

**CTA:**
- Button 1: "Gọi nhà hàng" (màu xanh dương outline)
  - Action: `tel:{restaurantPhone}`
- Button 2: "Chat với nhà hàng" (màu xám outline)
  - Action: Mở chat modal/component

---

### Trạng thái 2: DriverAssigned (Tài xế nhận đơn)

**Header:**
- Text: "Tài xế đã nhận đơn"
- Icon: `bi-person-check`
- Màu: Primary (xanh dương)

**Timeline:**
- Step 1-2: Completed
- Step 3: Active (có animation pulse)
- Step 4-5: Pending

**Dynamic Info:**
- Hiển thị:
  - Avatar tài xế (hình tròn, background xanh dương)
  - Tên tài xế
  - Button "Gọi" (màu xanh dương, icon phone)
- Layout: Flex, align-items-center

**CTA:**
- Button 1: "Gọi tài xế" (màu xanh dương primary)
  - Action: `tel:{driverPhone}`
- Button 2: "Chat với tài xế" (màu xanh dương outline)
  - Action: Mở chat modal/component
- Button 3: "Xem bản đồ" (màu xám outline)
  - Action: Mở Google Maps với địa chỉ giao hàng

---

### Trạng thái 4: Delivering (Đang giao hàng)

**Header:**
- Text: "Tài xế đang giao hàng"
- Icon: `bi-truck`
- Màu: Primary (xanh dương)

**Timeline:**
- Step 1-3: Completed
- Step 4: Active (có animation pulse)
- Step 5: Pending

**Dynamic Info:**
- Giống trạng thái 2 (DriverAssigned)
- Có thể thêm: "Khoảng cách còn lại: X km" (nếu có API location)

**CTA:**
- Giống trạng thái 2
- Thêm: "Theo dõi vị trí" (nếu có real-time location)

---

### Trạng thái 5: Completed (Hoàn thành)

**Header:**
- Text: "Đơn hàng đã hoàn thành"
- Icon: `bi-check-circle`
- Màu: Success (xanh lá)
- Có button "X" để đóng card

**Timeline:**
- Step 1-5: Tất cả Completed

**Dynamic Info:**
- Alert success: "Đơn hàng đã được giao thành công!"

**CTA:**
- Button 1: "Đánh giá đơn hàng" (màu vàng warning)
  - Action: Redirect đến `/Order/Rate/{orderId}`
- Button 2: "Đặt lại món này" (màu xanh dương outline)
  - Action: Redirect đến `/Shop` với filter sản phẩm

**Behavior:**
- Card vẫn hiển thị để user có thể đánh giá
- User có thể đóng card bằng button "X"
- Khi đóng, lưu vào localStorage để không hiển thị lại

---

### Trạng thái 6: Cancelled (Đã hủy)

**Header:**
- Text: "Đơn hàng đã hủy"
- Icon: `bi-x-circle`
- Màu: Danger (đỏ)

**Timeline:**
- Step 1: Completed (đã đặt)
- Step 2-5: Pending (không tiếp tục)

**Dynamic Info:**
- Alert danger: "Đơn hàng đã bị hủy"
- Hiển thị lý do hủy (nếu có): "Lý do: {cancelReason}"

**CTA:**
- Không có CTA (hoặc có thể có "Đặt lại món")

---

### Trạng thái 7: Failed (Thất bại)

**Header:**
- Text: "Đơn hàng không thể thực hiện"
- Icon: `bi-exclamation-triangle`
- Màu: Danger (đỏ)

**Timeline:**
- Tương tự Cancelled

**Dynamic Info:**
- Alert danger: "Đơn hàng không thể thực hiện"
- Hiển thị lý do thất bại (nếu có): "Lý do: {failureReason}"

**CTA:**
- Button: "Liên hệ hỗ trợ" hoặc "Đặt lại món"

---

## 🔄 Real-time Updates

### Cơ chế cập nhật:

1. **Polling** (mặc định):
   - Gọi API `/api/orders/active` mỗi 10 giây
   - So sánh status cũ vs mới
   - Nếu khác → Reload card HTML

2. **SignalR** (nếu có):
   - Listen event: `OrderStatusUpdated`
   - Khi có event → Reload card ngay lập tức
   - Fallback về polling nếu SignalR disconnect

### Khi trạng thái thay đổi:

1. **Header cập nhật:**
   - Text, icon, màu sắc thay đổi
   - Transition: `0.3s ease`

2. **Timeline cập nhật:**
   - Step mới → Active (có animation pulse)
   - Step cũ → Completed
   - Progress line cập nhật màu

3. **Dynamic Info cập nhật:**
   - Thông tin mới xuất hiện
   - Thông tin cũ biến mất
   - Transition: `fade-in` effect

4. **CTA cập nhật:**
   - Buttons mới xuất hiện
   - Buttons cũ biến mất
   - Transition: `slide-up` effect

---

## 📱 Responsive Design

### Mobile (< 576px):

- Card: Full width, padding `px-3`
- Timeline icons: 32px (thay vì 40px)
- Font sizes: Giảm 10-15%
- Buttons: Full width (`d-grid gap-2`)
- Sticky: Card sticky ở top khi scroll

### Tablet (576px - 992px):

- Card: Max-width 600px, centered
- Timeline icons: 36px
- Buttons: Có thể side-by-side nếu đủ chỗ

### Desktop (> 992px):

- Card: Max-width 600px, không sticky
- Timeline icons: 40px
- Buttons: Có thể side-by-side

---

## ⚠️ Xử lý trạng thái đặc biệt

### 1. Đơn bị trễ:

- Hiển thị alert warning trong Dynamic Info
- Text: "Đơn hàng có thể bị trễ. Lý do: {delayReason}"
- Thời gian dự kiến mới: Cập nhật màu đỏ
- CTA: "Liên hệ hỗ trợ"

### 2. Không tìm được tài xế:

- Hiển thị alert warning
- Text: "Đang tìm tài xế phù hợp. Vui lòng đợi..."
- Timeline: Dừng ở Step 2 (Chuẩn bị món)
- CTA: "Hủy đơn" hoặc "Liên hệ hỗ trợ"

### 3. Đơn hoàn thành:

- Card vẫn hiển thị để user đánh giá
- Có button "X" để đóng
- Khi đóng → Lưu vào localStorage
- Không tự động biến mất

---

## 🎯 Nguyên tắc UX

1. **Một card = Một đơn hàng:**
   - Chỉ hiển thị đơn hàng đang active (status 0-4)
   - Hoặc đơn hàng vừa hoàn thành (status 5) nếu chưa đóng

2. **Rõ ràng, không mơ hồ:**
   - Text ngắn gọn, dễ hiểu
   - Icon rõ ràng
   - Màu sắc phân biệt rõ

3. **Thao tác 1 tay:**
   - Buttons đủ lớn (min-height 44px)
   - Spacing đủ rộng
   - Dễ chạm trên mobile

4. **Real-time nhưng không làm phiền:**
   - Updates mượt mà, không giật lag
   - Không có sound/notification popup
   - User có thể đóng card khi muốn

---

## 📦 Implementation Notes

### File Structure:

```
Views/Shared/_OrderTrackingCard.cshtml  → Component HTML
Controllers/OrderController.cs          → API endpoints
Services/OrderService.cs                 → Business logic
wwwroot/js/order-tracking.js            → JavaScript logic (optional)
```

### API Endpoints cần có:

1. `GET /api/orders/active`
   - Trả về đơn hàng đang active của user hiện tại
   - Response: `{ success: true, order: {...} }`

2. `GET /Order/GetTrackingCard/{orderId}`
   - Trả về HTML của tracking card
   - Response: HTML string

3. `POST /api/orders/{orderId}/cancel`
   - Hủy đơn hàng
   - Response: `{ success: true, message: "..." }`

### SignalR Hub (nếu có):

- Hub name: `OrderHub`
- Event: `OrderStatusUpdated(orderId, status)`
- Client listen: `connection.on('OrderStatusUpdated', ...)`

---

## ✅ Checklist Implementation

- [x] Component HTML/CSS
- [x] Timeline với 5 bước
- [x] Dynamic info theo từng trạng thái
- [x] CTA buttons theo ngữ cảnh
- [x] Real-time updates (polling)
- [x] SignalR integration (optional)
- [x] Responsive design
- [x] Sticky positioning trên mobile
- [x] Xử lý trạng thái đặc biệt
- [x] Close button cho completed orders
- [x] LocalStorage để lưu trạng thái đóng

---

## 🚀 Next Steps

1. Tạo API endpoints trong `OrderController`
2. Tích hợp SignalR Hub (nếu cần)
3. Test trên các thiết bị mobile khác nhau
4. Tối ưu performance (debounce polling nếu cần)
5. Thêm analytics tracking cho các actions

