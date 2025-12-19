# Kế hoạch triển khai hệ thống đánh giá (Review System)

## 📊 Đánh giá tiến độ hiện tại

### ✅ Đã hoàn thành

1. **Database Schema**
   - ✅ Bảng `order_reviews`: Đánh giá đơn hàng (order_rating, driver_rating, comment, images, admin_reply)
   - ✅ Bảng `product_reviews`: Đánh giá sản phẩm (rating, comment, status)
   - ✅ Foreign keys và constraints đã được thiết lập
   - ✅ Indexes cho performance

2. **Backend - Order Reviews**
   - ✅ `OrderReviewService`: Service quản lý đánh giá đơn hàng
   - ✅ `OrderReviewsController`: API endpoints cho order reviews
   - ✅ Chức năng:
     - ✅ Tạo đánh giá đơn hàng (POST /api/orderreviews)
     - ✅ Lấy đánh giá của đơn hàng (GET /api/orderreviews/order/{orderId})
     - ✅ Lấy đánh giá của tài xế (GET /api/orderreviews/driver/{driverId})
     - ✅ Admin trả lời đánh giá (POST /api/orderreviews/{orderId}/reply)
     - ✅ Thống kê rating tài xế (GET /api/orderreviews/driver/{driverId}/stats)
   - ✅ Tự động cập nhật driver rating khi có đánh giá mới

3. **Frontend - Order Review**
   - ✅ Trang đánh giá đơn hàng: `/Order/Review/{id}`
   - ✅ View: `Views/Order/Review.cshtml` (có form đánh giá với upload ảnh)

4. **Admin - Reviews Page**
   - ✅ Trang Admin Reviews: `/Admin/Reviews`
   - ✅ View: `Views/Admin/Reviews.cshtml` (hiện tại là static template, chưa kết nối backend)

### ⚠️ Cần sửa/cập nhật

1. **Status Check không đúng**
   - ❌ `OrderReviewService.CanReviewOrderAsync`: Đang check `status == 5` (theo enum cũ)
   - ❌ `OrderStatusHelper.CanReview`: Đang check `status == 5`
   - ✅ **Cần sửa**: Theo OrderStatusHelper mới, status 4 = "Hoàn thành", nên phải check `status == 4`

2. **Product Reviews chưa có**
   - ❌ Chưa có `ProductReviewService`
   - ❌ Chưa có `ProductReviewsController`
   - ❌ Chưa có API endpoints cho product reviews

3. **Hiển thị reviews dưới sản phẩm**
   - ❌ Trang Product Detail chưa hiển thị reviews
   - ❌ Chưa có component hiển thị danh sách reviews
   - ❌ Chưa có form để user đánh giá sản phẩm

4. **Admin Reviews Page chưa hoàn thiện**
   - ❌ View hiện tại là static template
   - ❌ Chưa kết nối API để load danh sách reviews
   - ❌ Chưa có filter/search
   - ❌ Chưa có chức năng approve/reject product reviews
   - ❌ Chưa có tổng hợp thống kê

---

## 📋 Kế hoạch triển khai chi tiết

### **Bước 1: Sửa Status Check cho Order Reviews** ⚡ (Ưu tiên cao)

**Mục tiêu**: Đảm bảo user có thể đánh giá đơn hàng khi status = 4 (Hoàn thành) theo OrderStatusHelper mới.

**Công việc**:

1. **Sửa `OrderReviewService.CanReviewOrderAsync`**
   - File: `Services/OrderReviewService.cs`
   - Dòng 38: Đổi `result.status == 5` → `result.status == 4`
   - Comment: Cập nhật comment để phản ánh status mới

2. **Sửa `OrderStatusHelper.CanReview`**
   - File: `Services/OrderStatusHelper.cs`
   - Dòng 77-78: Đổi `return dbStatus == 5;` → `return dbStatus == 4;`
   - Comment: Cập nhật comment

3. **Kiểm tra các nơi khác sử dụng**
   - Tìm tất cả nơi check `status == 5` liên quan đến review
   - Đảm bảo consistency

**Thời gian ước tính**: 15 phút

---

### **Bước 2: Tạo ProductReviewService và Controller** 🔨

**Mục tiêu**: Xây dựng backend cho chức năng đánh giá sản phẩm.

**Công việc**:

1. **Tạo Interface `IProductReviewService`**
   - File: `Services/Interfaces/IProductReviewService.cs`
   - Methods:
     ```csharp
     Task<bool> CanReviewProductAsync(long productId, long userId);
     Task<ServiceResult> CreateReviewAsync(CreateProductReviewViewModel dto, long userId);
     Task<ProductReview?> GetProductReviewAsync(long productId, long userId);
     Task<List<ProductReview>> GetProductReviewsAsync(long productId, int page = 1, int pageSize = 10);
     Task<ServiceResult> UpdateReviewStatusAsync(long reviewId, short status, long adminId); // 0=pending, 1=approved
     Task<ServiceResult> DeleteReviewAsync(long reviewId, long userId);
     Task<ProductReviewStats> GetProductReviewStatsAsync(long productId);
     ```

2. **Tạo `ProductReviewService`**
   - File: `Services/ProductReviewService.cs`
   - Implement các methods từ interface
   - Logic:
     - `CanReviewProductAsync`: Check user đã mua sản phẩm (có order với product đó, status = 4) và chưa review
     - `CreateReviewAsync`: Insert vào `product_reviews`, update product rating
     - `GetProductReviewsAsync`: Lấy reviews đã approved (status = 1), có phân trang
     - `UpdateReviewStatusAsync`: Admin approve/reject review
     - `GetProductReviewStats`: Tính average rating, total reviews, rating distribution

3. **Tạo `ProductReviewsController`**
   - File: `Controllers/ProductReviewsController.cs`
   - Endpoints:
     - `POST /api/productreviews` - Tạo đánh giá
     - `GET /api/productreviews/product/{productId}` - Lấy danh sách reviews của sản phẩm
     - `GET /api/productreviews/product/{productId}/stats` - Thống kê reviews
     - `GET /api/productreviews/user/{userId}` - Lấy reviews của user
     - `PUT /api/productreviews/{reviewId}/status` - Admin approve/reject (role: admin)
     - `DELETE /api/productreviews/{reviewId}` - User xóa review của mình

4. **Tạo ViewModels**
   - File: `Models/CreateProductReviewViewModel.cs`
     ```csharp
     public class CreateProductReviewViewModel
     {
         public long ProductId { get; set; }
         public short Rating { get; set; } // 1-5
         public string? Comment { get; set; }
     }
     ```
   - File: `Models/ProductReviewStats.cs`
     ```csharp
     public class ProductReviewStats
     {
         public long ProductId { get; set; }
         public int TotalReviews { get; set; }
         public decimal AverageRating { get; set; }
         public Dictionary<int, int> RatingDistribution { get; set; } // 1-5 stars
     }
     ```

5. **Register Service trong `Program.cs`**
   - Thêm: `builder.Services.AddScoped<IProductReviewService, ProductReviewService>();`

**Thời gian ước tính**: 2-3 giờ

---

### **Bước 3: Hiển thị Reviews dưới sản phẩm** 🎨

**Mục tiêu**: User có thể xem và tạo đánh giá sản phẩm trên trang Product Detail.

**Công việc**:

1. **Cập nhật Product Detail Page**
   - File: `Views/Product/Detail.cshtml`
   - Thêm section "Đánh giá sản phẩm" sau thông tin sản phẩm
   - Hiển thị:
     - Average rating với stars
     - Total reviews count
     - Rating distribution (1-5 sao)
     - Danh sách reviews (phân trang)
     - Form để user đánh giá (nếu đã mua và chưa review)

2. **Tạo Partial View cho Review List**
   - File: `Views/Shared/_ProductReviews.cshtml`
   - Component hiển thị:
     - User avatar, name
     - Rating stars
     - Comment
     - Created date
     - Admin reply (nếu có)

3. **Tạo Partial View cho Review Form**
   - File: `Views/Shared/_ProductReviewForm.cshtml`
   - Form:
     - Star rating selector (1-5)
     - Textarea cho comment
     - Submit button
   - Validation: Chỉ hiển thị nếu user đã mua và chưa review

4. **JavaScript Integration**
   - Load reviews khi vào trang (AJAX)
   - Submit review form (AJAX)
   - Update UI sau khi submit thành công
   - Pagination cho reviews list

5. **Cập nhật Product Model/ViewModel**
   - Thêm properties: `AverageRating`, `TotalReviews`, `CanReview`
   - Load từ API khi render page

**Thời gian ước tính**: 3-4 giờ

---

### **Bước 4: Tích hợp Product Reviews vào Order Review Flow** 🔄

**Mục tiêu**: Khi user đánh giá đơn hàng, có thể đánh giá từng sản phẩm trong đơn.

**Công việc**:

1. **Cập nhật Order Review Page**
   - File: `Views/Order/Review.cshtml`
   - Thêm section "Đánh giá sản phẩm" sau phần đánh giá đơn hàng/tài xế
   - Hiển thị danh sách sản phẩm trong đơn
   - Mỗi sản phẩm có form đánh giá riêng (nếu chưa review)

2. **Cập nhật CreateReviewViewModel**
   - Thêm: `List<ProductReviewItem> ProductReviews`
   ```csharp
   public class ProductReviewItem
   {
       public long ProductId { get; set; }
       public short Rating { get; set; }
       public string? Comment { get; set; }
   }
   ```

3. **Cập nhật OrderReviewService**
   - Khi tạo order review, nếu có product reviews, tạo luôn product reviews
   - Transaction để đảm bảo atomicity

4. **UI/UX**
   - Collapsible section cho product reviews
   - Optional: User có thể bỏ qua, chỉ đánh giá đơn hàng
   - Hiển thị trạng thái: "Đã đánh giá" / "Chưa đánh giá" cho mỗi sản phẩm

**Thời gian ước tính**: 2-3 giờ

---

### **Bước 5: Hoàn thiện Admin Reviews Page** 👨‍💼

**Mục tiêu**: Admin có thể quản lý tất cả reviews (order + product), filter, search, approve/reject.

**Công việc**:

1. **Tạo API Endpoints cho Admin**
   - File: `Controllers/AdminReviewsController.cs` (hoặc thêm vào `AdminController`)
   - Endpoints:
     - `GET /api/admin/reviews` - Lấy tất cả reviews (order + product) với filter
       - Query params: `type` (order/product), `status` (pending/approved), `rating`, `page`, `pageSize`
     - `GET /api/admin/reviews/stats` - Tổng hợp thống kê
     - `PUT /api/admin/reviews/product/{reviewId}/status` - Approve/reject product review
     - `POST /api/admin/reviews/order/{orderId}/reply` - Trả lời order review (đã có)

2. **Cập nhật Admin Reviews View**
   - File: `Views/Admin/Reviews.cshtml`
   - Thay thế static template bằng dynamic content
   - Features:
     - Tabs: "Order Reviews" / "Product Reviews"
     - Search box (theo product name, user name, comment)
     - Filter: Rating (1-5), Status (All/Pending/Approved), Date range
     - Table hiển thị:
       - Order Reviews: Order code, User, Order rating, Driver rating, Comment, Date, Actions
       - Product Reviews: Product name, User, Rating, Comment, Status, Date, Actions
     - Actions: Approve, Reject, Reply (order), Delete
     - Pagination

3. **JavaScript Integration**
   - Load reviews từ API
   - Filter/search real-time
   - Approve/reject với confirmation
   - Update UI sau actions

4. **Thống kê Dashboard**
   - Tổng số reviews
   - Average rating (order + product)
   - Rating distribution
   - Reviews theo thời gian (chart)

**Thời gian ước tính**: 4-5 giờ

---

### **Bước 6: Testing và Polish** ✅

**Mục tiêu**: Đảm bảo tất cả chức năng hoạt động đúng, UI/UX tốt.

**Công việc**:

1. **Unit Tests** (Optional)
   - Test ProductReviewService methods
   - Test validation logic

2. **Integration Tests**
   - Test API endpoints
   - Test review flow end-to-end

3. **UI/UX Improvements**
   - Responsive design cho mobile
   - Loading states
   - Error handling và messages
   - Toast notifications

4. **Performance**
   - Optimize queries (indexes đã có)
   - Caching nếu cần (product stats)
   - Lazy loading reviews

**Thời gian ước tính**: 2-3 giờ

---

## 📊 Tổng kết

### Thời gian ước tính tổng cộng: **13-18 giờ**

### Thứ tự ưu tiên:
1. ⚡ **Bước 1** (15 phút) - Sửa status check
2. 🔨 **Bước 2** (2-3 giờ) - ProductReviewService
3. 🎨 **Bước 3** (3-4 giờ) - Hiển thị reviews dưới sản phẩm
4. 🔄 **Bước 4** (2-3 giờ) - Tích hợp vào order review
5. 👨‍💼 **Bước 5** (4-5 giờ) - Admin page
6. ✅ **Bước 6** (2-3 giờ) - Testing

### Lưu ý:
- Bước 1 nên làm ngay vì ảnh hưởng đến chức năng hiện tại
- Bước 2-3 có thể làm song song nếu có 2 dev
- Bước 4 phụ thuộc vào Bước 2-3
- Bước 5 có thể làm độc lập sau khi Bước 2 hoàn thành

---

## 🔍 Schema Reference

### `order_reviews` table:
```sql
- id (bigint, PK)
- order_id (bigint, FK, UNIQUE)
- user_id (bigint, FK)
- driver_id (bigint, FK, nullable)
- order_rating (smallint, 1-5)
- driver_rating (smallint, 1-5, nullable)
- comment (text, nullable)
- images (text[], nullable)
- admin_reply (text, nullable)
- admin_replied_at (timestamp, nullable)
- created_at, updated_at
```

### `product_reviews` table:
```sql
- id (bigint, PK)
- product_id (bigint, FK)
- user_id (bigint, FK)
- rating (smallint, 1-5)
- comment (text, nullable)
- status (smallint, 0=pending, 1=approved)
- created_at, updated_at
- UNIQUE (product_id, user_id) -- Mỗi user chỉ review 1 lần/sản phẩm
```

---

## 📝 Notes

- Status mapping: Theo OrderStatusHelper mới (1-6), status 4 = "Hoàn thành"
- Product reviews cần admin approve (status = 1) mới hiển thị công khai
- Order reviews hiển thị ngay (không cần approve)
- Driver rating tự động update khi có review mới
- Product rating cần tính lại khi có review mới (approved)

