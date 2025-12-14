CÁC BƯỚC THỰC HIỆN
🔹 GIAI ĐOẠN 1: CẬP NHẬT DATABASE (30 phút)
Bước 1.1: Cập nhật comments cho orders.status
Đã có constraint 0-7
Cần đảm bảo mapping: 0=pending, 1=confirmed, 2=driver_assigned, 3=picking_up, 4=delivering, 5=completed, 6=cancelled, 7=refunded
Bước 1.2: Seed dữ liệu driver
Insert thêm 2-3 tài khoản driver vào users (role='driver')
Insert tương ứng vào bảng drivers

🔹 GIAI ĐOẠN 2: BACKEND - MODELS & VIEWMODELS (30 phút)
Bước 2.1: Tạo/Cập nhật Models
Driver.cs - Model cho tài xế
OrderReview.cs - Model đánh giá
PaymentTransaction.cs - Model giao dịch
OrderStatusLog.cs - Model log trạng thái
Cập nhật Order.cs: thêm navigation properties (Driver, Review, PaymentTransactions, StatusLogs)
Bước 2.2: Tạo ViewModels
DriverOrderViewModel - Hiển thị đơn hàng cho tài xế (có thông tin khách hàng, địa chỉ, items)
OrderTrackingViewModel - Tracking realtime cho user (status timeline, driver info, location)
PaymentRequestViewModel - Request tạo thanh toán
PaymentCallbackViewModel - Response từ VNPay/Momo
CreateReviewViewModel - Tạo đánh giá
DriverStatsViewModel - Thống kê tài xế

🔹 GIAI ĐOẠN 3: BACKEND - SERVICES (2-3 giờ)
Bước 3.1: DriverService
Interface IDriverService:

GetAvailableOrdersAsync() - Lấy đơn chưa có tài xế (status=1)
GetMyOrdersAsync(driverId) - Lấy đơn của tài xế (status=2,3,4)
GetOrderDetailAsync(orderId, driverId) - Chi tiết 1 đơn
AcceptOrderAsync(orderId, driverId) - Nhận đơn (1→2, cập nhật driver_id)
StartPickupAsync(orderId, driverId) - Bắt đầu lấy hàng (2→3)
StartDeliveryAsync(orderId, driverId) - Bắt đầu giao (3→4)
CompleteOrderAsync(orderId, driverId) - Hoàn thành (4→5, cập nhật completed_at, tăng total_orders)
UpdateStatusAsync(driverId, status) - Đổi trạng thái tài xế (available/offline/busy)
GetDriverStatsAsync(driverId) - Thống kê tài xế
Implementation:

Dùng Dapper query database
Validate: chỉ driver được assign mới được thao tác
Log mỗi thay đổi vào order_status_logs
Transaction để đảm bảo consistency
Bước 3.2: OrderTrackingService
Interface IOrderTrackingService:

GetOrderTrackingAsync(orderId, userId?) - Lấy tracking (validate user chỉ xem đơn mình)
GetOrderHistoryAsync(orderId) - Lấy timeline từ order_status_logs
UpdateOrderStatusAsync(orderId, newStatus, notes, userId) - Admin update status
CanCancelOrderAsync(orderId, userId) - Check có thể hủy không (status < 3)
CancelOrderAsync(orderId, userId, reason) - Hủy đơn (→6, release inventory, refund nếu đã thanh toán)
SendStatusNotificationAsync(orderId, status) - Gửi notification realtime
Implementation:

State machine cho status transitions hợp lệ
Validate ownership (user/driver/admin)
Auto release inventory khi cancel
Integrate SignalR để push notification
Bước 3.3: PaymentService (VNPay & Momo)
Interface IPaymentService:

CreateVNPayPaymentAsync(orderId, returnUrl, ipAddress) - Tạo URL thanh toán VNPay
CreateMomoPaymentAsync(orderId, returnUrl, ipAddress) - Tạo URL thanh toán Momo
ProcessVNPayReturnAsync(queryParams) - Xử lý redirect về từ VNPay
ProcessVNPayIPNAsync(queryParams) - Xử lý IPN callback
ProcessMomoReturnAsync(postData) - Xử lý redirect về từ Momo
ProcessMomoIPNAsync(postData) - Xử lý IPN callback
ProcessPaymentSuccessAsync(orderId, transactionId) - Update order khi thanh toán thành công
ProcessPaymentFailureAsync(orderId, errorMessage) - Xử lý thất bại
RefundPaymentAsync(orderId, reason) - Hoàn tiền
Implementation:

Generate secure signature theo doc VNPay/Momo
Validate signature khi nhận callback
Lưu đầy đủ request/response vào payment_transactions
Idempotency để tránh duplicate processing
Config keys trong appsettings.json
Bước 3.4: OrderReviewService
Interface IOrderReviewService:

CanReviewOrderAsync(orderId, userId) - Check có thể review không (status=5, chưa review)
CreateReviewAsync(dto) - Tạo review
GetOrderReviewAsync(orderId) - Lấy review của 1 đơn
GetDriverReviewsAsync(driverId, page, pageSize) - Lấy reviews của tài xế
AdminReplyAsync(reviewId, reply, adminId) - Admin trả lời review
Implementation:

Validate order completed
1 order chỉ review 1 lần
Auto update driver rating (average)
Upload images to cloud storage nếu có
Bước 3.5: NotificationService (SignalR)
Interface INotificationService:

NotifyOrderStatusChangedAsync(orderId, status, message)
NotifyDriverAssignedAsync(orderId, driverInfo)
NotifyOrderCancelledAsync(orderId, reason)
Implementation:
Sử dụng IHubContext<OrderHub>
Send to group Order_{orderId}

🔹 GIAI ĐOẠN 4: BACKEND - API CONTROLLERS (1-2 giờ)
Bước 4.1: DriversController
Endpoints:

GET /api/drivers/available-orders - Xem đơn khả dụng
GET /api/drivers/my-orders - Xem đơn của mình
GET /api/drivers/orders/{id} - Chi tiết đơn
POST /api/drivers/orders/{id}/accept - Nhận đơn
POST /api/drivers/orders/{id}/start-pickup - Bắt đầu lấy hàng
POST /api/drivers/orders/{id}/start-delivery - Bắt đầu giao
POST /api/drivers/orders/{id}/complete - Hoàn thành
PUT /api/drivers/status - Cập nhật status (available/offline)
GET /api/drivers/stats - Thống kê
Authorization: [Authorize(Roles = "driver")]

Bước 4.2: OrderTrackingController
Endpoints:

GET /api/orders/{id}/tracking - Tracking info (user/driver/admin)
GET /api/orders/{id}/history - Timeline
POST /api/orders/{id}/cancel - Hủy đơn (validate permission)
PUT /api/orders/{id}/status - Admin update status
Authorization: [Authorize] + validate ownership

Bước 4.3: PaymentController
Endpoints:

POST /api/payment/vnpay/create - Tạo VNPay payment
GET /api/payment/vnpay/return - User redirect về (AllowAnonymous)
POST /api/payment/vnpay/ipn - IPN callback (AllowAnonymous)
POST /api/payment/momo/create - Tạo Momo payment
GET /api/payment/momo/return - User redirect về
POST /api/payment/momo/ipn - IPN callback
POST /api/payment/orders/{id}/refund - Admin refund
Security: Validate signature, idempotency key

Bước 4.4: OrderReviewsController
Endpoints:

POST /api/reviews - Tạo review
GET /api/reviews/order/{orderId} - Lấy review
GET /api/reviews/driver/{driverId} - Lấy reviews của driver
POST /api/reviews/{id}/reply - Admin reply
Authorization: User chỉ review đơn của mình

🔹 GIAI ĐOẠN 5: BACKEND - REALTIME (SIGNALR) (1 giờ)
Bước 5.1: Tạo OrderHub
Hub class kế thừa Hub
Methods: JoinOrderGroup(orderId), LeaveOrderGroup(orderId)
Configure SignalR trong Program.cs
Map hub: app.MapHub<OrderHub>("/hubs/order")
Bước 5.2: Integrate vào Services
Inject IHubContext<OrderHub> vào DriverService, OrderTrackingService
Gọi SendAsync("OrderStatusUpdated", data) khi status thay đổi
Send to group: Order_{orderId}

🔹 GIAI ĐOẠN 6: FRONTEND - VIEWS (ALPINE.JS) (3-4 giờ)
Bước 6.1: Driver Dashboard (/Driver/Dashboard)
UI Components:

Header: Toggle status (Available/Offline), Stats card (today orders, earnings, rating)
Tabs: Available Orders | My Orders
Order cards:
Order code, customer name, phone, address
Items list, total price
Action buttons: Accept / Start Pickup / Start Delivery / Complete
Map link (Google Maps)
Alpine.js Data:

Methods:

loadAvailableOrders() - Fetch từ API
loadMyOrders() - Fetch từ API
acceptOrder(orderId) - POST accept
startDelivery(orderId) - POST start-delivery
completeOrder(orderId) - POST complete
updateStatus(newStatus) - PUT status
Auto refresh mỗi 30s
Bước 6.2: Order Tracking Page (/Orders/Tracking/{id})
UI Components:

Status timeline (visual progress bar: pending → confirmed → driver_assigned → picking_up → delivering → completed)
Driver info card (avatar, name, phone, rating, call button)
Order details (items, prices, address)
History timeline (từ order_status_logs)
Cancel button (nếu status < 3)
Alpine.js Data:

Methods:

loadOrder() - Fetch tracking
connectSignalR() - Connect & join group
cancelOrder() - POST cancel
Listen SignalR: connection.on("OrderStatusUpdated")
Bước 6.3: Payment Checkout Page (/Checkout/Payment)
UI Components:

Order summary (items, subtotal, discount, shipping, total)
Shipping address form (nếu chưa có)
Payment method selection: COD | VNPay | Momo (radio buttons)
Confirm button
Alpine.js Data:

Methods:

selectPaymentMethod(method) - Toggle payment
calculateShipping() - Tính phí ship theo zone
submitOrder() - Nếu COD: create order, nếu VNPay/Momo: create payment → redirect
Bước 6.4: Order Review Page (/Orders/{id}/Review)
UI Components:

Order info summary
Star rating cho order (1-5 stars)
Star rating cho driver (1-5 stars)
Comment textarea
Upload images (max 5, preview thumbnails)
Submit button
Alpine.js Data:

Methods:

selectRating(type, value) - Click star
uploadImages(event) - Handle file upload
submitReview() - POST review
Bước 6.5: User Order History (/Orders/MyOrders)
UI Components:

Filter tabs: All | Pending | Delivering | Completed | Cancelled
Order cards:
Order code, date, status badge
Items preview, total
Actions: View Tracking | Cancel (nếu được phép) | Review (nếu completed)
Alpine.js Data:

Methods:

loadOrders(filter) - Fetch orders
viewTracking(orderId) - Navigate to tracking
cancelOrder(orderId) - POST cancel
reviewOrder(orderId) - Navigate to review


🔹 GIAI ĐOẠN 7: TESTING (2-3 giờ)
Bước 7.1: Manual Testing - Full Flow
User Flow:

User đăng nhập → thêm sản phẩm vào giỏ → checkout
Chọn địa chỉ → chọn VNPay → redirect to VNPay sandbox
Thanh toán test → return về website → order created (status=1)
View tracking page → thấy status "confirmed"
Driver Flow:

Driver login → set status "Available"
Xem available orders → click Accept → status → 2
Click "Start Pickup" → status → 3
Click "Start Delivery" → status → 4
Click "Complete" → status → 5, driver stats tăng
User Review:

User vào completed orders → click Review
Cho 5 sao order, 5 sao driver, viết comment
Submit → driver rating được update
Cancel Flow:

User tạo order COD → status=1
View tracking → click Cancel (trước khi có driver)
Confirm → status=6, inventory released
Bước 7.2: Unit Tests
Test Classes:

DriverServiceTests - Test accept, complete, stats calculation
OrderTrackingServiceTests - Test status transitions, cancel logic
PaymentServiceTests - Test signature generation, validation
OrderReviewServiceTests - Test rating calculation
Mocking:

Mock NpgsqlConnection
Mock IHubContext<OrderHub>
Use xUnit + Moq
Bước 7.3: Integration Tests
Test Scenarios:

Full order workflow: cart → checkout → payment → driver accept → complete → review
Cancel order flow: create → cancel → check inventory restored
Payment callback flow: create payment → mock IPN callback → verify order updated
🔹 GIAI ĐOẠN 8: DEPLOYMENT & MONITORING (1 giờ)
Bước 8.1: Configuration
Cập nhật appsettings.Production.json:
VNPay/Momo production credentials
Database connection string (production)
SignalR Redis backplane (nếu scale-out)
CORS domains
Bước 8.2: Logging & Monitoring
Structured logging với Serilog
Log mọi payment transaction
Log mọi status transition
Monitor payment success rate
Monitor order completion rate
Bước 8.3: Documentation
API documentation (Swagger)
Database schema diagram
Flow diagrams (user, driver, payment)
Testing checklist
📝 CHECKLIST TỔNG HỢP
Database ✅
 Đã có đủ bảng
 Seed driver accounts
Backend Services ✅
 DriverService (8 methods)
 OrderTrackingService (6 methods)
 PaymentService - VNPay (6 methods)
 PaymentService - Momo (6 methods)
 OrderReviewService (5 methods)
 NotificationService (3 methods)
API Controllers ✅
 DriversController (8 endpoints)
 OrderTrackingController (4 endpoints)
 PaymentController (7 endpoints)
 OrderReviewsController (4 endpoints)
Frontend Views ✅
 Driver Dashboard
 Order Tracking Page (với SignalR)
 Payment Checkout Page
 Order Review Page
 User Order History
SignalR ✅
 OrderHub
 Integrate vào services
 Frontend connection handling
Testing ✅
 Manual testing full flow
 Unit tests cho services
 Integration tests
Deployment ✅
 Production config
 Logging setup
 Documentation
⏱️ TỔNG THỜI GIAN ƯỚC TÍNH
Database: 30 phút (seed data)
Backend Models: 30 phút
Backend Services: 2-3 giờ (6 services)
Backend Controllers: 1-2 giờ (4 controllers)
SignalR: 1 giờ
Frontend Views: 3-4 giờ (5 pages)
Testing: 2-3 giờ
Deployment: 1 giờ
TỔNG: 11-15 giờ làm việc

🎯 ƯU TIÊN THỰC HIỆN
Phase 1 (Core): Database → Models → DriverService → DriversController → Driver Dashboard → Test driver accept/complete flow

Phase 2 (Tracking): OrderTrackingService → OrderTrackingController → SignalR → Tracking Page → Test realtime updates

Phase 3 (Payment): PaymentService → PaymentController → Payment Page → Test VNPay/Momo integration

Phase 4 (Review): OrderReviewService → ReviewsController → Review Page → Test review & rating

Phase 5 (Polish): Testing → Deployment → Documentation

Bạn muốn bắt đầu từ Phase nào? Tôi recommend Phase 1 để có MVP nhanh nhất! 🚀