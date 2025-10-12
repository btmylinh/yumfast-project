# Hướng dẫn viết Prompt hiệu quả cho AI trong Development

## Mục lục
1. [Mẹo Prompt cho các loại dự án](#mẹo-prompt-cho-các-loại-dự-án)
2. [Xây dựng dự án từ đầu](#xây-dựng-dự-án-từ-đầu)
3. [Quản lý TODO và chia Sessions hiệu quả](#quản-lý-todo-và-chia-sessions-hiệu-quả)
4. [Phân tích code có sẵn và làm tiếp](#phân-tích-code-có-sẵn-và-làm-tiếp)
5. [Prompt cho các mô hình dự án khác nhau](#prompt-cho-các-mô-hình-dự-án-khác-nhau)

---

## Mẹo Prompt cho các loại dự án

### FRONTEND - Giao diện người dùng

#### Làm giao diện
```
✅ TỐT: "Làm thanh menu cho web bán hàng có logo, danh mục sản phẩm, ô tìm kiếm, giỏ hàng. Dùng Bootstrap, phải responsive trên điện thoại"

❌ TRÁNH: "Làm menu đẹp"
```

#### Tạo component
```
✅ TỐT: "Tạo thẻ sản phẩm hiển thị tên, giá, hình ảnh, nút thêm vào giỏ. Có hiệu ứng khi rê chuột, sao đánh giá, nhãn giảm giá"

❌ TRÁNH: "Tạo thẻ sản phẩm"
```

#### Làm hiệu ứng
```
✅ TỐT: "Làm nút quay lên đầu trang, xuất hiện mượt mà khi cuộn xuống quá 300px, dùng CSS và JavaScript thuần"

❌ TRÁNH: "Làm nút scroll"
```

### BACKEND - Phần xử lý phía sau

#### Cơ sở dữ liệu
```
✅ TỐT: "Tạo bảng Product trong Entity Framework có Id, Tên (bắt buộc, tối đa 200 ký tự), Giá, Danh mục, Ngày tạo. Có ràng buộc và liên kết bảng"

❌ TRÁNH: "Tạo bảng sản phẩm"
```

#### Làm API
```
✅ TỐT: "Làm API lấy danh sách sản phẩm có phân trang, lọc theo danh mục và giá, sắp xếp theo tên/giá. Trả về kèm tổng số. Có xử lý lỗi"

❌ TRÁNH: "Làm API sản phẩm"
```

#### Đăng nhập bảo mật
```
✅ TỐT: "Làm hệ thống đăng nhập JWT cho ASP.NET Core. Có làm mới token, phân quyền theo vai trò, mã hóa mật khẩu BCrypt"

❌ TRÁNH: "Làm đăng nhập"
```

### FULLSTACK - Kết nối 2 phần

#### Kết nối API
```
✅ TỐT: "Tạo hook React lấy danh sách sản phẩm từ API. Có trạng thái loading, xử lý lỗi, cache dữ liệu, tự động thử lại khi lỗi"

❌ TRÁNH: "Kết nối frontend backend"
```

#### Xử lý form
```
✅ TỐT: "Làm form đăng ký có kiểm tra email đúng định dạng, mật khẩu mạnh, xác nhận mật khẩu. Frontend dùng React Hook Form, Backend dùng validation"

❌ TRÁNH: "Làm form đăng ký"
```

### SỬA LỖI VÀ TỐI ƯU

#### Sửa lỗi
```
✅ TỐT: "Sửa lỗi 'không đọc được thuộc tính' ở dòng 45 trong ProductList.tsx. Đang duyệt mảng products từ API. Cần kiểm tra null và giao diện dự phòng"

❌ TRÁNH: "Sửa lỗi này"
```

#### Tăng tốc
```
✅ TỐT: "Tối ưu trang hiển thị 1000+ sản phẩm: chỉ load những cái hiển thị, lazy load hình ảnh, cache kết quả, tìm kiếm có delay"

❌ TRÁNH: "Làm cho nhanh hơn"
```

### Các mẫu lệnh hay dùng

#### Kiểm tra code
```
"Xem lại code này và góp ý về:
1. Lỗ hổng bảo mật
2. Hiệu suất chậm
3. Cách tổ chức code  
4. Cách làm hay
5. Xử lý lỗi
[dán code vào]"
```

#### Viết test
```
"Viết test cho [tên function/component] bao gồm:
- Các trường hợp bình thường
- Trường hợp biên  
- Trường hợp lỗi
- Mock các thứ phụ thuộc
Dùng [Jest/xUnit/NUnit]"
```

#### Viết tài liệu
```
"Viết tài liệu cho API này:
- Mục đích và cách dùng
- Dữ liệu gửi/nhận
- Mã lỗi và thông báo
- Ví dụ code (curl, JavaScript)  
- Giới hạn tần suất"
```

---

## Quản lý TODO và chia Sessions hiệu quả

### Khả năng xử lý tối ưu của AI

#### 1 SESSION có thể handle tốt:
- 3-5 todos đơn giản (sửa CSS, thêm field, fix button)
- 2-3 todos phức tạp (tạo component, API endpoint, logic business)  
- 1-2 todos rất phức tạp (tạo toàn bộ trang với CRUD đầy đủ)

#### Cho 1 trang giao diện + API hoàn chỉnh:
- Frontend: 4-6 todos (layout, components, forms, validation, styling, responsive)
- Backend: 3-4 todos (model, controller, API endpoints, validation)
- Integration: 2-3 todos (connect API, error handling, loading states)

**TỔNG: 9-13 todos → Nên chia thành 3-4 sessions**

---

### Cách thực hiện cơ bản

#### **BƯỚC 1: Prompt phân tích và chia Sessions phù hợp**

```
"Phân tích yêu cầu và chia sessions cho tôi:

YÊU CẦU TỔNG QUAN:
[Mô tả chi tiết yêu cầu của bạn]

TECHNICAL CONTEXT:
- Framework: [ASP.NET/React/Flutter/Frontend-only/etc.]
- Libraries available: [Bootstrap, Alpine.js, jQuery, etc.]
- Reference pages: [Product, Category pages for design consistency]
- Database schema: [Nếu có]

PHÂN TÍCH VÀ ĐƯA RA:
1. Phân loại workload thành các nhóm (Data, Logic, UI, Polish)
2. Ước lượng số todos cho từng nhóm
3. Đề xuất số sessions phù hợp (thường 3-5 sessions)
4. Outline từng session với focus area cụ thể
5. Lưu ý những task nặng cần tách session riêng

QUY TẮC:
- Max 4 todos/session 
- Session cuối chỉ polish/testing, không add features
- Đa ngôn ngữ và complex animations tách session riêng
- Mỗi session có focus area rõ ràng
- Không rigid 4 sessions - tùy độ phức tạp project

OUTPUT FORMAT:
Trả về session plan theo format chuẩn với CONTEXT, TODOS, PRIORITY, SUCCESS CRITERIA"
```

#### **BƯỚC 2: Kiểm tra tinh chỉnh và làm theo từng session**

Sau khi nhận được session plan từ BƯỚC 1:

1. **Kiểm tra và tinh chỉnh plan:**
   - Xem có session nào quá nặng không (>4 todos)
   - Đảm bảo Session 4 chỉ polish/testing
   - Tách riêng đa ngôn ngữ nếu có

2. **Thực hiện từng session theo plan:**
   - Copy exact prompt từ session plan
   - Mark todo as in-progress trước khi bắt đầu
   - Complete từng todo và mark completed
   - Kiểm tra SUCCESS CRITERIA trước khi chuyển session tiếp theo

3. **Template thực hiện session:**
```
"Session [X/Y] - [PHASE NAME] cho [TÊN TRANG/FEATURE]:

CONTEXT: 
- [Brief về Session trước đã làm gì]
- [Files/components hiện có]

TODOS CHO SESSION NÀY (2-4 items tùy độ phức tạp):
1. [Todo cụ thể với acceptance criteria]
2. [Todo cụ thể với acceptance criteria]  
3. [Todo cụ thể với acceptance criteria]
4. [Todo cụ thể với acceptance criteria - optional]

PRIORITY: [High → Low theo thứ tự]

TECHNICAL REQUIREMENTS:
- Framework: [ASP.NET Core/React/Flutter/etc]
- Patterns: [MVC/API/Component-based]
- Libraries: [Entity Framework/Tailwind/etc]
- Conventions: [Naming/File structure]

SUCCESS CRITERIA:
- [Cách test/verify từng todo]
- [Expected output cho từng item]

NEXT SESSION NOTES:
- [Preparation cho session tiếp theo]"
``` 


---

**QUY TẮC QUAN TRỌNG**:
- **Không bao giờ quá 4 todos trong 1 session**
- **1 session = 1 focus area** (Data/Logic/UI/Polish)
- **Session cuối chỉ polish & testing, KHÔNG add features mới**
- **Features nặng = Sessions riêng** (đa ngôn ngữ, animations)
- **Đa ngôn ngữ luôn là Session 5+** (không bao giờ Session cuối)
- **Luôn có SUCCESS CRITERIA** cụ thể và đo được
- **Số sessions tùy độ phức tạp** - không rigid 4 sessions

### Framework cho Session cuối chuẩn:
```
SESSION CUỐI: Final Polish & Testing
TODOS:
1. Code cleanup (remove unused, console.logs, comments)
2. Cross-browser testing (Chrome, Firefox, Safari, Edge)  
3. Performance audit & bug fixes

KHÔNG BAO GIỜ TRONG SESSION CUỐI:
- Đa ngôn ngữ (quá nặng, dễ lỗi)
- Complex animations (ảnh hưởng performance)
- New features (bulk actions, export, etc.)
- Major UI changes
```

---
### Ví dụ 1: Frontend-Only Banner Management (DOM + Alpine.js)

#### Yêu cầu tổng quan:
```
Tạo trang Banner Management chỉ frontend DOM với:
- Mock data với LocalStorage
- CRUD interface (Create, Read, Update, Delete)
- Tìm kiếm real-time
- Lọc theo status (Active/Inactive)
- Phân trang client-side
- Đa ngôn ngữ (vi/en) 
- Bootstrap + Alpine.js + existing CSS libraries
- Kế thừa design system từ Product/Category pages
- Responsive design
```

#### **BƯỚC 1: Prompt phân tích và chia Sessions phù hợp**

##### **Template phân tích workload:**
```
"Phân tích yêu cầu và chia sessions cho tôi:

YÊU CẦU TỔNG QUAN:
[Mô tả chi tiết yêu cầu của bạn]

TECHNICAL CONTEXT:
- Framework: [ASP.NET/React/Flutter/Frontend-only/etc.]
- Libraries available: [Bootstrap, Alpine.js, jQuery, etc.]
- Reference pages: [Product, Category pages for design consistency]
- Database schema: [Nếu có]

PHÂN TÍCH VÀ ĐƯA RA:
1. Phân loại workload thành các nhóm (Data, Logic, UI, Polish)
2. Ước lượng số todos cho từng nhóm
3. Đề xuất số sessions phù hợp (thường 3-5 sessions)
4. Outline từng session với focus area cụ thể
5. Lưu ý những task nặng cần tách session riêng

QUY TẮC:
- Max 4 todos/session 
- Session cuối chỉ polish/testing, không add features
- Đa ngôn ngữ và complex animations tách session riêng
- Mỗi session có focus area rõ ràng
- Không rigid 4 sessions - tùy độ phức tạp project

OUTPUT FORMAT:
Trả về session plan theo format chuẩn với CONTEXT, TODOS, PRIORITY, SUCCESS CRITERIA"
```

##### **Ví dụ thực tế với Banner Management:**
```
"Phân tích yêu cầu và chia sessions cho tôi:

YÊU CẦU TỔNG QUAN:
Tạo trang Banner Management frontend-only với:
- CRUD đầy đủ (Create, Read, Update, Delete)
- Tìm kiếm theo tên banner real-time
- Lọc theo status (Active/Inactive)
- Phân trang client-side
- Đa ngôn ngữ (vi/en)
- Responsive design
- Kế thừa design từ Product/Category pages

TECHNICAL CONTEXT:
- Framework: Frontend-only (HTML, CSS, JS)
- Libraries available: Bootstrap 5, Alpine.js, existing CSS
- Reference pages: Product và Category management pages
- Database schema: banners (id, name, image, link, status)
- Data persistence: LocalStorage

PHÂN TÍCH VÀ ĐƯA RA:
1. Phân loại workload thành các nhóm
2. Ước lượng số todos cho từng nhóm  
3. Đề xuất số sessions tối ưu
4. Outline từng session với focus area
5. Lưu ý task nặng cần tách riêng

QUY TẮC:
- Max 4 todos/session
- Session 4 chỉ polish/testing
- Đa ngôn ngữ = Session 5 riêng
- Mỗi session có focus rõ ràng"
```

**Kết trả về thực hiện từng session ở bước 2**
```
PHÂN TÍCH WORKLOAD:
- HTML Structure & Layout: 3 todos
- Alpine.js Logic & Data: 4 todos  
- UI Components & Interactions: 4 todos
- Final Polish & Testing: 3 todos
- Localization (Optional): 3 todos

TỔNG: 14 todos core + 3 todos optional → 4 sessions + 1 optional

SESSION PLAN:
Session 1: HTML Structure & Layout (3 todos)
Session 2: Alpine.js Data & Core Logic (4 todos)
Session 3: Search, Filter & Pagination (4 todos)
Session 4: Final Polish & Testing (3 todos)
Session 5: Localization (Optional - 3 todos)

[Tiếp theo sẽ detail từng session ở bước 2]
```

---

#### **BƯỚC 2: Sessions chi tiết (sau khi đã có plan từ BƯỚC 1)**

##### **SESSION 1: HTML Structure & Layout (3 todos)**

```
"Session 1/4 - HTML Structure cho Banner Management (Frontend-only):

CONTEXT: 
- Dự án frontend với Bootstrap, Alpine.js có sẵn
- Đã có Product/Category pages làm reference design
- Cần tạo Banner management page chỉ với DOM

TODOS:
1. Tạo HTML structure với Bootstrap layout (copy từ Product page)
2. Setup table structure cho banner list với responsive classes
3. Tạo modal forms cho Create/Edit banner

PRIORITY: Layout → Table → Modal

TECHNICAL REQUIREMENTS:
- Bootstrap 5 grid system và components
- Responsive design: col-12, col-md-8, col-lg-6
- Copy header/sidebar structure từ existing pages
- Modal với backdrop và keyboard support
- Table với sorting indicators
- Form validation styling classes

SUCCESS CRITERIA:
- HTML render đúng layout với sidebar navigation
- Table structure responsive trên mobile/desktop
- Modal show/hide correctly với Bootstrap JS
- Forms có proper validation styling
- Consistent spacing với existing pages

NEXT SESSION NOTES:
- Setup Alpine.js data và methods
- Mock data structure chuẩn bị"
```

##### **SESSION 2: Alpine.js Data & Core Logic (4 todos)**

```
"Session 2/4 - Alpine.js Logic cho Banner Management:

CONTEXT: 
- Session 1 đã hoàn thành: HTML structure working
- Modal forms render correctly
- Cần implement Alpine.js data binding và logic

TODOS:
1. Setup Alpine.js data structure với mock banners array
2. Implement CRUD methods (create, update, delete) với LocalStorage
3. Setup reactive data binding cho table và forms
4. Implement form validation với Alpine.js

PRIORITY: Data Setup → CRUD Methods → Data Binding → Validation

TECHNICAL REQUIREMENTS:
- Alpine.js x-data cho component state
- LocalStorage để persist data
- Mock data: 10-15 sample banners
- Validation rules: name required, image URL format
- Two-way binding với x-model
- Event handlers với x-on:click

MOCK DATA STRUCTURE:
{
  id: 1,
  name: "Hero Banner 1", 
  image: "/images/banner1.jpg",
  link: "https://example.com",
  status: 1,
  created_at: "2025-10-10"
}

SUCCESS CRITERIA:
- Alpine.js component initialize correctly
- CRUD operations working với LocalStorage
- Form data binding responsive
- Validation trigger on submit
- Data persist qua page refresh

NEXT SESSION NOTES:
- Implement search và filter functionality
- Pagination logic"
```

#### **SESSION 3: Search, Filter & Pagination (4 todos)**

```
"Session 3/4 - Interactive Features cho Banner Management:

CONTEXT:
- Session 2 đã hoàn thành: CRUD operations working
- Data binding và validation functional
- Cần add search, filter, pagination

TODOS:
1. Implement real-time search theo banner name
2. Implement status filter (All/Active/Inactive) với dropdown
3. Implement client-side pagination với page controls
4. Add sorting functionality cho table columns

PRIORITY: Search → Filter → Pagination → Sorting

TECHNICAL REQUIREMENTS:
- Debounced search với Alpine.js (300ms delay)
- Computed properties cho filtered data
- Pagination: 5 items per page, page controls
- Status filter với Bootstrap dropdown
- Sort arrows trong table headers
- URL params để maintain state

ALPINE.JS METHODS:
- filteredBanners() computed
- searchBanners(query) method
- filterByStatus(status) method  
- paginatedData() computed
- sortBy(column) method

SUCCESS CRITERIA:
- Search filter results real-time
- Status dropdown filter working
- Pagination navigate correctly
- Sort by name/status/date working
- URL reflects current filter state
- Empty states show appropriate messages

NEXT SESSION NOTES:
- Final polish và testing only
- No new features in Session 4"
```

##### **SESSION 4: Final Polish & Testing (3 todos) - QUAN TRỌNG**

```
"Session 4/4 - Final Polish & Testing cho Banner Management:

CONTEXT:
- Session 3 đã hoàn thành: Search, filter, pagination working
- All interactive features functional
- Cần final polish và testing, KHÔNG thêm features mới

TODOS:
1. Code cleanup và optimization (remove unused code, console.logs)
2. Cross-browser testing và minor compatibility fixes
3. Final performance audit và bug fixes

PRIORITY: Cleanup → Testing → Performance

TECHNICAL REQUIREMENTS:
- Test trên Chrome, Firefox, Safari, Edge
- Mobile responsive testing
- Performance check với browser dev tools
- Code review cho best practices

SUCCESS CRITERIA:
- No console errors across browsers
- Mobile experience smooth
- Page load < 2 seconds
- All features working consistently
- Clean, maintainable code

FINAL DELIVERABLE:
- Production-ready Banner Management (frontend-only)
- Cross-browser compatible
- Performance optimized
- Clean codebase

LƯU Ý: Session 4 chỉ polish/testing, không add features mới"
```

TODOS:
1. Code cleanup và optimization (remove unused code, console.logs)
2. Cross-browser testing và minor compatibility fixes
3. Final performance audit và bug fixes

PRIORITY: Cleanup → Testing → Performance

TECHNICAL REQUIREMENTS:
- Test trên Chrome, Firefox, Safari, Edge
- Mobile responsive testing
- Performance check với browser dev tools
- Code review cho best practices

SUCCESS CRITERIA:
- No console errors across browsers
- Mobile experience smooth
- Page load < 2 seconds
- All features working consistently
- Clean, maintainable code

FINAL DELIVERABLE:
- Production-ready Banner Management (frontend-only)
- Cross-browser compatible
- Performance optimized
- Clean codebase

LƯU Ý: Session 4 chỉ polish/testing, không add features mới"
```

##### Session 5: Localization (tùy chọn)
```
"Session 5 - Đa ngôn ngữ cho Banner Management:

TODOS:
1. Setup translation object trong Alpine.js
2. Implement language switching logic
3. Update all static text với dynamic translations
4. Test language persistence

LƯU Ý: Chỉ làm khi core features đã stable và tested"
```

##### Session 6: Advanced Animations (nếu cần)
```
"Session 6 - Animations cho Banner Management:

TODOS:
1. Add smooth CSS transitions cho modals
2. Loading animations cho CRUD operations
3. Hover effects và micro-interactions
4. Performance test animations

LƯU Ý: Chỉ làm khi có thời gian và không ảnh hưởng performance"
```

---

### Ví dụ 2: Backend-Only Banner Management (API, đã có frontend)

#### Yêu cầu tổng quan:
```
Trang Banner Management đã có frontend hoàn chỉnh, cần:
- Tạo các API CRUD endpoints 
- Tích hợp API với frontend có sẵn
- API hỗ trợ: tìm kiếm real-time, lọc status, phân trang
- 2 trường hợp: Frontend có mock data vs Frontend chỉ DOM tĩnh
```

#### **TRƯỜNG HỢP A: Frontend đã có mock data + Alpine.js logic**

**BƯỚC 1: Prompt phân tích và chia Sessions**

```
"Phân tích yêu cầu Backend API cho Banner Management:

YÊU CẦU TỔNG QUAN:
Frontend Banner Management đã hoàn thành với:
- Alpine.js components với mock data
- CRUD operations working với LocalStorage  
- Search, filter, pagination đã implement
- Cần replace mock data bằng real API calls

TECHNICAL CONTEXT:
- Framework: ASP.NET Core Web API
- Frontend: Alpine.js với fetch() calls
- Database: Entity Framework Core + SQL Server
- Authentication: JWT hoặc Cookie-based
- Existing: HomeController, AuthController patterns

PHÂN TÍCH VÀ ĐƯA RA:
1. Phân loại workload: API Development, Database Setup, Frontend Integration
2. Ước lượng todos: 
   - API Layer: 4 todos (model, controller, endpoints, validation)
   - Database: 2 todos (migration, seeding)
   - Integration: 3 todos (replace localStorage, error handling, authentication)
3. Đề xuất 3 sessions (Backend-focused)
4. Session focus: Database → API → Integration

QUY TẮC:
- Max 4 todos/session
- Session cuối chỉ testing/polish
- Follow existing ASP.NET Core patterns
```

**Sessions chi tiết:**

**SESSION 1: Database & Models (3 todos)**
```
"Session 1/3 - Database Setup cho Banner Management API:

CONTEXT:
- Frontend Alpine.js đã ready với mock data structure
- Cần tạo backend API tương ứng với frontend expectations
- Follow existing Entity Framework patterns trong project

TODOS:
1. Tạo Banner entity model với properties phù hợp với frontend
2. Tạo migration và update DatabaseContext  
3. Seed sample data tương ứng với mock data hiện tại

PRIORITY: Model → Migration → Seeding

TECHNICAL REQUIREMENTS:
- Entity Framework Core với Code First approach
- Banner model: Id, Name, ImageUrl, LinkUrl, Status, CreatedAt, UpdatedAt
- Status enum: Active = 1, Inactive = 0
- Seed 10-15 sample banners tương tự mock data
- Follow naming convention của existing models

MOCK DATA REFERENCE (từ frontend):
{
  id: 1,
  name: "Hero Banner 1", 
  image: "/images/banner1.jpg",
  link: "https://example.com",
  status: 1,
  created_at: "2025-10-10"
}

SUCCESS CRITERIA:
- Banner table tạo thành công trong database
- Sample data seeded correctly
- EF Core context recognize Banner entity
- Migration chạy không lỗi

NEXT SESSION NOTES:
- Tạo BannerController với CRUD API endpoints
- Implement search, filter, pagination logic"
```

**SESSION 2: API Endpoints (4 todos)**
```
"Session 2/3 - API Endpoints cho Banner Management:

CONTEXT:
- Session 1 hoàn thành: Banner entity và database ready
- Frontend expect specific API contract từ mock implementation
- Cần tạo REST API endpoints theo chuẩn ASP.NET Core

TODOS:
1. Tạo BannerController với [ApiController] attributes
2. Implement CRUD endpoints (GET, POST, PUT, DELETE)
3. Implement search và filter logic trong GET endpoint
4. Implement pagination với query parameters

PRIORITY: Basic CRUD → Search/Filter → Pagination → Validation

TECHNICAL REQUIREMENTS:
- Inherit từ ControllerBase (API-only)
- Route: [Route("api/[controller]")]
- DTOs: BannerDto, CreateBannerDto, UpdateBannerDto
- Response format: { data, message, success, totalCount }
- Query params: search, status, page, pageSize, sortBy, sortOrder

API ENDPOINTS:
- GET /api/banners?search=hero&status=1&page=1&pageSize=5
- GET /api/banners/{id}
- POST /api/banners
- PUT /api/banners/{id}  
- DELETE /api/banners/{id}

FRONTEND CONTRACT (Alpine.js expects):
// GET Response
{
  "data": [
    { "id": 1, "name": "Hero Banner", "imageUrl": "...", "linkUrl": "...", "status": 1 }
  ],
  "message": "Success",
  "success": true,
  "totalCount": 25,
  "currentPage": 1,
  "totalPages": 5
}

SUCCESS CRITERIA:
- All CRUD operations working via Postman/Swagger
- Search trả về filtered results
- Status filter working (0=Inactive, 1=Active)
- Pagination metadata correct
- Error handling với proper HTTP status codes

NEXT SESSION NOTES:
- Replace frontend localStorage calls với API calls
- Add loading states và error handling"
```

**SESSION 3: Frontend Integration (3 todos)**
```
"Session 3/3 - Frontend Integration với API:

CONTEXT:
- Session 2 hoàn thành: API endpoints working
- Frontend Alpine.js có đầy đủ UI logic
- Cần replace localStorage operations với fetch() calls

TODOS:
1. Replace Alpine.js localStorage methods với API fetch calls
2. Add loading states và error handling cho API calls
3. Update authentication headers và CSRF tokens nếu cần

PRIORITY: Replace Storage → Error Handling → Authentication

TECHNICAL REQUIREMENTS:
- Keep existing Alpine.js component structure
- Replace methods: loadBanners(), createBanner(), updateBanner(), deleteBanner()
- Add loading states: isLoading reactive property
- Error handling với user-friendly messages
- CSRF token cho POST/PUT/DELETE requests

ALPINE.JS UPDATES:
```javascript
// Before (localStorage)
async loadBanners() {
    this.banners = JSON.parse(localStorage.getItem('banners')) || [];
}

// After (API)
async loadBanners() {
    this.isLoading = true;
    try {
        const response = await fetch('/api/banners?search=' + this.searchQuery + '&status=' + this.statusFilter);
        const result = await response.json();
        this.banners = result.data;
        this.totalCount = result.totalCount;
    } catch (error) {
        this.showError('Failed to load banners');
    } finally {
        this.isLoading = false;
    }
}
```

SUCCESS CRITERIA:
- Frontend hoàn toàn sử dụng API thay vì localStorage
- Loading indicators hiển thị khi call API
- Error messages hiển thị khi API fails
- Search, filter, pagination working với real data
- CRUD operations persist trong database

FINAL DELIVERABLE:
- Production-ready Banner Management với real backend
- Frontend Alpine.js integrated với ASP.NET Core API
- Full CRUD với search, filter, pagination"
```

---

#### **TRƯỜNG HỢP B: Frontend chỉ có DOM tĩnh (không có logic)**

**BƯỚC 1: Prompt phân tích**

```
"Phân tích yêu cầu Full Backend + Frontend Integration:

YÊU CẦU TỔNG QUAN:
Frontend chỉ có HTML/CSS static, cần:
- Tạo complete backend API
- Implement frontend JavaScript logic từ đầu
- Integrate API với frontend mới tạo
- Full functionality: CRUD, search, filter, pagination

TECHNICAL CONTEXT:
- Framework: ASP.NET Core Web API + Vanilla JS/Alpine.js
- Frontend: Static HTML cần add interactive logic
- Database: Entity Framework Core
- No existing JavaScript logic

PHÂN TÍCH:
1. Workload groups: Backend API, Frontend Logic, Integration, Polish
2. Todo estimation: 
   - Backend: 4 todos (database + API)
   - Frontend Logic: 4 todos (Alpine.js implementation)  
   - Integration: 3 todos (connect API)
   - Polish: 2 todos (testing + optimization)
3. Đề xuất 4 sessions (Full-stack)
4. Focus areas: Backend → Frontend → Integration → Polish

SESSION PLAN:
Session 1: Backend API Development (4 todos)
Session 2: Frontend Alpine.js Logic (4 todos)
Session 3: API Integration (3 todos)
Session 4: Testing & Polish (2 todos)
```

**Sessions chi tiết:**

**SESSION 1: Complete Backend API (4 todos)**
```
"Session 1/4 - Complete Backend API cho Banner Management:

CONTEXT:
- Frontend chỉ có static HTML/CSS
- Cần tạo complete backend từ database đến API
- Follow existing ASP.NET Core project patterns

TODOS:
1. Tạo Banner entity, DbContext update, migration
2. Tạo BannerController với full CRUD endpoints
3. Implement advanced query features (search, filter, sort, pagination)
4. Add validation, error handling, và response formatting

PRIORITY: Database → Basic CRUD → Advanced Features → Error Handling

TECHNICAL REQUIREMENTS:
- Entity Framework Core with Banner entity
- RESTful API với proper HTTP methods
- Query parameters: ?search=&status=&page=&pageSize=&sortBy=&sortOrder=
- Response format: { data: [], message: "", success: bool, totalCount: int }
- Validation attributes và ModelState checking
- Global error handling middleware

API SPECIFICATION:
- GET /api/banners - List với query support
- GET /api/banners/{id} - Single banner
- POST /api/banners - Create new  
- PUT /api/banners/{id} - Update existing
- DELETE /api/banners/{id} - Delete banner

VALIDATION RULES:
- Name: Required, MaxLength(200)
- ImageUrl: Required, URL format
- LinkUrl: Optional, URL format  
- Status: Required, 0 or 1

SUCCESS CRITERIA:
- Database created với sample data
- All endpoints working via Postman
- Search by name working
- Status filter working (Active/Inactive)
- Pagination returning correct metadata
- Validation errors returned properly

NEXT SESSION NOTES:
- Frontend sẽ consume những APIs này
- Cần response format consistent"
```

**SESSION 2: Frontend JavaScript Logic (4 todos)**
```
"Session 2/4 - Frontend Alpine.js Logic cho Banner Management:

CONTEXT:
- Session 1 hoàn thành: Backend API ready và tested
- Frontend có static HTML structure
- Cần implement Alpine.js logic để tạo interactive interface

TODOS:
1. Setup Alpine.js component với data structure
2. Implement CRUD UI methods (create, edit, delete workflows)
3. Implement search và filter functionality với debouncing
4. Implement pagination logic với page controls

PRIORITY: Component Setup → CRUD UI → Search/Filter → Pagination

TECHNICAL REQUIREMENTS:
- Alpine.js x-data component cho state management
- Modal forms cho Create/Edit banner workflows
- Real-time search với 300ms debounce
- Status dropdown filter với "All", "Active", "Inactive"
- Pagination controls với Previous/Next và page numbers
- Form validation on client-side

ALPINE.JS STRUCTURE:
```javascript
function bannerManager() {
    return {
        // State
        banners: [],
        searchQuery: '',
        statusFilter: 'all',
        currentPage: 1,
        pageSize: 5,
        totalCount: 0,
        isLoading: false,
        
        // Modal state  
        showModal: false,
        editingBanner: null,
        
        // Methods sẽ implement
        init() { /* Initialize component */ },
        loadBanners() { /* Load data */ },
        searchBanners() { /* Search logic */ },
        filterByStatus() { /* Filter logic */ },
        createBanner() { /* Create workflow */ },
        editBanner() { /* Edit workflow */ },
        deleteBanner() { /* Delete workflow */ }
    }
}
```

UI INTERACTIONS:
- Search input với x-model và debounce
- Status dropdown với x-on:change
- Pagination buttons với x-on:click
- Modal forms với Bootstrap modal controls
- Loading states với x-show directives

SUCCESS CRITERIA:
- Alpine.js component initialize correctly
- Modal forms show/hide properly
- Search input debouncing working  
- Status filter dropdown functional
- Pagination controls interactive
- All UI workflows complete (create/edit/delete)
- Client-side validation working

NEXT SESSION NOTES:
- Sẽ connect với real API thay vì mock data
- Cần error handling cho API calls"
```

**SESSION 3: API Integration (3 todos)**
```
"Session 3/4 - API Integration cho Banner Management:

CONTEXT:
- Session 1: Backend API hoàn thành và tested
- Session 2: Frontend logic hoàn thành với mock data
- Cần connect frontend với real API

TODOS:
1. Replace mock data calls bằng real fetch() API calls
2. Implement loading states và error handling cho tất cả API calls
3. Add authentication headers và CSRF protection nếu cần

PRIORITY: API Integration → Loading States → Error Handling

TECHNICAL REQUIREMENTS:
- Replace Alpine.js mock methods với fetch() calls
- Add loading indicators cho UX
- Error handling với toast notifications hoặc alert messages
- CSRF tokens cho state-changing operations
- Response parsing và data mapping

API INTEGRATION EXAMPLES:
```javascript
// Load banners với query parameters
async loadBanners() {
    this.isLoading = true;
    try {
        const url = `/api/banners?search=${this.searchQuery}&status=${this.statusFilter}&page=${this.currentPage}&pageSize=${this.pageSize}`;
        const response = await fetch(url);
        const result = await response.json();
        
        if (result.success) {
            this.banners = result.data;
            this.totalCount = result.totalCount;
        } else {
            this.showError(result.message);
        }
    } catch (error) {
        this.showError('Failed to load banners');
    } finally {
        this.isLoading = false;
    }
}

// Create banner
async createBanner(formData) {
    try {
        const response = await fetch('/api/banners', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': document.querySelector('meta[name="csrf-token"]').content
            },
            body: JSON.stringify(formData)
        });
        
        const result = await response.json();
        if (result.success) {
            this.loadBanners(); // Refresh list
            this.closeModal();
            this.showSuccess('Banner created successfully');
        } else {
            this.showError(result.message);
        }
    } catch (error) {
        this.showError('Failed to create banner');
    }
}
```
    }
}
```

ERROR HANDLING:
- Network errors với retry mechanism
- Validation errors hiển thị per field
- Server errors với generic error message
- Loading timeout sau 30 seconds

SUCCESS CRITERIA:
- All CRUD operations working với real API
- Search results update real-time từ server
- Filter và pagination working với API
- Loading indicators show during API calls
- Error messages hiển thị appropriately
- Success notifications cho user actions
- Data persists across page refreshes

NEXT SESSION NOTES:
- Final testing và performance optimization
- Cross-browser compatibility check"
```

**SESSION 4: Testing & Polish (2 todos)**
```
"Session 4/4 - Final Testing & Polish cho Banner Management:

CONTEXT:
- Session 3 hoàn thành: Full integration working
- API và Frontend hoàn toàn connected
- Cần final polish và comprehensive testing

TODOS:
1. Comprehensive testing: functionality, browser compatibility, performance
2. Code cleanup, optimization, và documentation

PRIORITY: Testing → Optimization

TECHNICAL REQUIREMENTS:
- Test all CRUD operations thoroughly
- Cross-browser testing (Chrome, Firefox, Safari, Edge)
- Mobile responsive testing
- Performance optimization
- Code cleanup và comments

TESTING CHECKLIST:
✅ CRUD Operations:
- Create banner với valid/invalid data
- Edit banner với validation
- Delete banner với confirmation
- View banner details

✅ Search & Filter:
- Search results accuracy
- Real-time search performance
- Status filter combinations
- Empty state handling

✅ Pagination:
- Page navigation working
- Correct item counts
- Edge cases (first/last page)

✅ Error Scenarios:
- Network failures
- Server errors
- Validation failures
- Empty states

✅ Performance:
- Page load time < 2 seconds
- API response time < 500ms
- Smooth animations
- No memory leaks

OPTIMIZATION:
- Remove console.logs
- Minimize API calls
- Optimize images
- Clean unused CSS/JS

SUCCESS CRITERIA:
- All functionality working perfectly
- Cross-browser compatibility confirmed
- Mobile experience smooth
- Performance benchmarks met
- Clean, documented code
- Production-ready application

FINAL DELIVERABLE:
- Complete Banner Management system
- Backend: ASP.NET Core API với Entity Framework
- Frontend: Alpine.js với Bootstrap UI
- Full CRUD, search, filter, pagination
- Production-ready với error handling"
```

---

#### **So sánh 2 trường hợp:**

| Aspect | Trường hợp A (Có logic) | Trường hợp B (DOM tĩnh) |
|--------|------------------------|------------------------|
| **Sessions** | 3 sessions | 4 sessions |
| **Focus** | Backend-heavy | Full-stack balanced |
| **Frontend work** | Integration only | Complete implementation |
| **Timeline** | Nhanh hơn | Lâu hơn nhưng comprehensive |
| **Risk** | Thấp (logic đã test) | Cao hơn (nhiều moving parts) |

#### **Khi nào dùng approach nào:**

**Trường hợp A - Frontend có logic sẵn:**
- Prototype nhanh đã có
- Logic đã được test thoroughly
- Chỉ cần persist data
- Timeline ngắn

**Trường hợp B - DOM tĩnh:**
- Yêu cầu production-grade
- Cần full control over logic
- Integration testing từ đầu
- Long-term maintainability

---

## Xây dựng dự án từ đầu

### Tình huống: Đã có sẵn danh sách yêu cầu chức năng, database, công nghệ

Khi bắt đầu một dự án hoàn toàn mới, cần **chia nhỏ và có hệ thống** để làm việc hiệu quả.

---

### **BƯỚC 1: Prompt phân tích và lên kế hoạch tổng thể theo hướng đối tượng**

#### **Template phân tích dự án từ đầu:**
```
"Phân tích yêu cầu dự án theo hướng đối tượng và đưa ra kế hoạch làm việc:

THÔNG TIN DỰ ÁN:
- Tên dự án: [Tên dự án]
- Loại ứng dụng: [Web/Mobile/Desktop]
- Mục tiêu: [Bán hàng/Quản lý/Blog/etc.]

CHỨC NĂNG CẦN LÀM:
[Paste danh sách tính năng đầy đủ]

CƠ SỞ DỮ LIỆU:
[Paste bảng dữ liệu hoặc mô tả]

CÔNG NGHỆ SỬ DỤNG:
- Backend: [ASP.NET/Node.js/Python/etc.]
- Frontend: [React/Vue/Razor/etc.]
- Database: [SQL Server/MySQL/etc.]
- Giao diện: [Bootstrap/Tailwind/etc.]

PHÂN TÍCH VÀ ĐƯA RA:
1. Chia dự án thành các phases lớn
2. Ước lượng số sessions cho từng phase
3. Thứ tự development optimal
4. Những phần khó cần chú ý
5. Risk assessment và mitigation strategies

QUY TẮC OUTPUT:
- Chỉ trả về 4 phases ngắn gọn
- Mỗi phase max 8 sessions
- Format: "Phase X: [Tên] - [Mô tả 1 dòng] - [X sessions]"
- Không giải thích thêm, chỉ 4 dòng phases
- Từ cơ bản đến nâng cao
- Tổng không quá 20 sessions

OUTPUT: 4 dòng phases, không giải thích thêm"
```


#### Ví dụ thực tế với dự án bán hàng:
```
"Phân tích yêu cầu dự án và đưa ra kế hoạch làm việc:

THÔNG TIN DỰ ÁN:
- Tên dự án: Shop Trái Cây Online
- Loại ứng dụng: Website bán hàng
- Mục tiêu: Bán trái cây online có quản trị

CHỨC NĂNG CẦN LÀM:
KHÁCH HÀNG:
- Đăng ký/đăng nhập
- Xem sản phẩm, tìm kiếm, lọc
- Chi tiết sản phẩm, đánh giá
- Giỏ hàng, thanh toán
- Theo dõi đơn hàng
- Quản lý tài khoản

QUẢN TRỊ:
- Dashboard thống kê
- Quản lý sản phẩm (CRUD)
- Quản lý danh mục
- Quản lý đơn hàng
- Quản lý khách hàng
- Báo cáo bán hàng

CƠ SỞ DỮ LIỆU:
- Users
- Categories
- Products 
- Orders 
- OrderItems 
- Reviews 

CÔNG NGHỆ SỬ DỤNG:
- Backend: ASP.NET Core
- Frontend: Razor Pages + Bootstrap + Alpine.js
- Database: SQL Server
- Thanh toán: Stripe/PayPal
- Upload file: Local storage

PHÂN TÍCH: Chia thành 4 phases, khoảng 24-30 sessions, phases 1 luôn là Foundation & Infrastructure authentication"
```

<small>*CRUD: Create/Read/Update/Delete (thêm/xem/sửa/xóa) | phases: giai đoạn*</small>

---

### **BƯỚC 2: Kế hoạch các giai đoạn development**

Sau khi có phân tích từ BƯỚC 1, sẽ có kế hoạch như sau:

#### **PHASE 1: Foundation & Infrastructure (6-8 sessions)**
```
FOCUS: Setup cơ sở hạ tầng và authentication
SESSIONS:
1. Project setup, database design, migrations
2. Authentication system (register/login/roles)
3. Basic models và repositories
4. Admin layout và navigation
5. User layout và navigation
6. Basic error handling và logging

DELIVERABLE: Working authentication với basic layouts
DEPENDENCIES: None
RISK LEVEL: Low
TIMELINE: 1-2 weeks
```

<small>*infrastructure: cơ sở hạ tầng | authentication: xác thực người dùng | migrations: file tạo bảng database tự động | repositories: lớp truy cập dữ liệu | deliverable: sản phẩm bàn giao | dependencies: phụ thuộc*</small>

#### **PHASE 2: Core Product Management (8-10 sessions)**
```
FOCUS: Product catalog và basic shopping functionality
SESSIONS:
1. Category management (admin)
2. Product CRUD (admin) 
3. File upload và image management
4. Customer product browsing
5. Search và filter functionality
6. Product details page
7. Shopping cart implementation
8. Basic inventory tracking

DELIVERABLE: Complete product catalog với shopping cart
DEPENDENCIES: Phase 1 completed
RISK LEVEL: Medium (complex search/filter)
TIMELINE: 2-3 weeks
```

<small>*catalog: danh mục sản phẩm | functionality: chức năng | browsing: duyệt xem | implementation: triển khai | inventory tracking: theo dõi kho hàng*</small>

#### **PHASE 3: Order Management & Business Logic (6-8 sessions)**
```
FOCUS: Order processing và customer experience
SESSIONS:
1. Checkout process design
2. Order creation và validation
3. Payment integration (Stripe/PayPal)
4. Order management (admin)
5. Customer order history
6. Email notifications
7. Order status tracking

DELIVERABLE: Complete order flow end-to-end
DEPENDENCIES: Phase 2 completed
RISK LEVEL: High (payment integration)
TIMELINE: 2-3 weeks
```

<small>*business logic: logic nghiệp vụ | processing: xử lý | checkout: thanh toán | validation: kiểm tra dữ liệu | integration: tích hợp | notifications: thông báo | end-to-end: từ đầu đến cuối*</small>

#### **PHASE 4: Advanced Features & Polish (4-6 sessions)**
```
FOCUS: User experience enhancement và optimization
SESSIONS:
1. Review system
2. Wishlist functionality
3. Dashboard analytics
4. Performance optimization
5. Mobile responsiveness
6. Final testing và bug fixes

DELIVERABLE: Production-ready application
DEPENDENCIES: Phase 3 completed
RISK LEVEL: Low
TIMELINE: 1-2 weeks
```

<small>*enhancement: cải tiến | optimization: tối ưu hóa | analytics: phân tích dữ liệu | responsiveness: tương thích thiết bị di động | production-ready: sẵn sàng triển khai thực tế*</small>

---

---

### **BƯỚC 3: Quy trình chia nhỏ từ Roadmap → Tasks cụ thể**

Sau khi có **roadmap phases** từ BƯỚC 2, cần chia nhỏ tiếp theo chuỗi prompts:
**Output prompt trước → Input prompt sau** cho đến khi có **tasks cụ thể**.

#### **3.1: Session Planning (Tối ưu hóa)**

Chia phase thành sessions cụ thể với output ngắn gọn.

**Template Prompt:**
```
"Với Phase [X]: [TÊN PHASE], chia thành sessions cụ thể:

INPUT: [KẾT QUẢ TỪ BƯỚC 1]

YÊU CẦU OUTPUT:
Session X.1: [Tên] - [Mục tiêu 1 dòng]
Session X.2: [Tên] - [Mục tiêu 1 dòng]
Session X.3: [Tên] - [Mục tiêu 1 dòng]
...

NGUYÊN TẮC:
- Mỗi session 1 mục tiêu rõ ràng
- Sessions liên kết tuần tự
- Chỉ trả về danh sách sessions, không chi tiết thêm"
```

#### **3.2: Session to Tasks (Tập trung thực thi)**

Chuyển session thành tasks cụ thể có thể thực hiện ngay.

**Template Prompt:**
```
"Chuyển đổi Session [X.Y]: [TÊN SESSION] thành 4 tasks cụ thể:

INPUT: [SESSION TỪ BƯỚC 3.1]

YÊU CẦU OUTPUT (Format TODO):
- [ ] Task 1: [Action cụ thể] - [File/Location] - [Kết quả expect]
- [ ] Task 2: [Action cụ thể] - [File/Location] - [Kết quả expect]  
- [ ] Task 3: [Action cụ thể] - [File/Location] - [Kết quả expect]
- [ ] Task 4: [Action cụ thể] - [File/Location] - [Kết quả expect]

NGUYÊN TẮC:
- Tasks thực hiện trong 30-60 phút
- Ghi rõ file/folder cần tạo/sửa
- Kết quả đo được cụ thể
- Không giải thích, chỉ list tasks
```

#### **3.3: Task Validation (Đảm bảo chất lượng)**

Kiểm tra tasks có đủ để hoàn thành session không.

**Template Prompt:**
```
"Kiểm tra 4 tasks sau có đủ để hoàn thành session không:

TASKS CẦN CHECK: [4 TASKS TỪ BƯỚC 3.2]

TIÊU CHÍ:
- Tasks cover đầy đủ session goal?
- Dependencies giữa tasks rõ ràng?
- Có thiếu setup/testing steps?

YÊU CẦU OUTPUT:
✅ APPROVED - Tasks đủ để hoàn thành session
❌ MISSING - [Task thiếu cần bổ sung]

Nếu MISSING, suggest thêm 1-2 tasks cần thiết
```

---

### **BƯỚC 4: Execute Tasks với TODO Sessions**

Sau khi có **4 executable todos** từ BƯỚC 3.2 (hoặc 3.3 nếu cần validation), dùng **"Quản lý TODO và chia Sessions hiệu quả"** để thực hiện:

**Template Integration:**
```
"Session [X.Y] - [Session Name]:

TODOS (từ BƯỚC 3.2):
1. [Todo 1 với acceptance criteria]
2. [Todo 2 với acceptance criteria]  
3. [Todo 3 với acceptance criteria]
4. [Todo 4 với acceptance criteria]

[Áp dụng template từ phần 'Quản lý TODO và chia Sessions hiệu quả']
```

---

### **VÍ DỤ THỰC TẾ: Shop Trái Cây Online**

Áp dụng quy trình 4 bước cho dự án cụ thể.

#### **BƯỚC 1: Phân tích dự án**

**Template Prompt:**
```
"Phân tích yêu cầu dự án và đưa ra kế hoạch làm việc:

THÔNG TIN DỰ ÁN:
- Tên dự án: Shop Trái Cây Online
- Loại ứng dụng: Website bán hàng
- Mục tiêu: Bán trái cây online có quản trị

CHỨC NĂNG CẦN LÀM:
KHÁCH HÀNG:
- Đăng ký/đăng nhập
- Xem sản phẩm, tìm kiếm, lọc
- Chi tiết sản phẩm, đánh giá
- Giỏ hàng, thanh toán
- Theo dõi đơn hàng

QUẢN TRỊ:
- Dashboard thống kê
- Quản lý sản phẩm (CRUD)
- Quản lý danh mục
- Quản lý đơn hàng
- Quản lý khách hàng

CƠ SỞ DỮ LIỆU:
- Users, Categories, Products, Orders, OrderItems, Reviews

CÔNG NGHỆ SỬ DỤNG:
- Backend: ASP.NET Core
- Frontend: Razor Pages + Bootstrap + Alpine.js
- Database: SQL Server
- Thanh toán: Stripe

QUY TẮC OUTPUT:
- Chỉ trả về 4 phases ngắn gọn
- Mỗi phase max 8 sessions
- Format: "Phase X: [Tên] - [Mô tả 1 dòng] - [X sessions]"
- Không giải thích thêm, chỉ 4 dòng phases
- Từ cơ bản đến nâng cao
- Tổng không quá 20 sessions

OUTPUT: 4 dòng phases, không giải thích thêm, Phase 1 Cơ sở hạ tầng và authentication
```

**Kết quả mong đợi:**
```
Phase 1: Foundation Setup - Cơ sở hạ tầng và authentication - 6 sessions
Phase 2: Product Management - Catalog và shopping cart - 7 sessions  
Phase 3: Order Processing - Checkout và payment - 5 sessions
Phase 4: Advanced Features - Dashboard và optimization - 4 sessions
```

#### **BƯỚC 3.1: Session Planning cho Phase 1**

**Template Prompt:**
```
"Với Phase 1: Foundation Setup - Cơ sở hạ tầng và authentication - 6 sessions, chia thành sessions cụ thể:

INPUT: Phase 1: Foundation Setup - Cơ sở hạ tầng và authentication - 6 sessions

YÊU CẦU OUTPUT:
Session 1.1: [Tên] - [Mục tiêu 1 dòng]
Session 1.2: [Tên] - [Mục tiêu 1 dòng]
Session 1.3: [Tên] - [Mục tiêu 1 dòng]
Session 1.4: [Tên] - [Mục tiêu 1 dòng]
Session 1.5: [Tên] - [Mục tiêu 1 dòng]
Session 1.6: [Tên] - [Mục tiêu 1 dòng]

NGUYÊN TẮC:
- Mỗi session 1 mục tiêu rõ ràng
- Sessions liên kết tuần tự
- Chỉ trả về danh sách sessions, không chi tiết thêm
```

**Kết quả mong đợi:**
```
Session 1.1: Project Setup - Tạo project và database foundation
Session 1.2: Entity Models - Tạo models và migrations  
Session 1.3: Authentication - Setup ASP.NET Identity và roles
Session 1.4: Auth UI - Login/register pages và navigation
Session 1.5: Admin Layout - Basic admin interface setup
Session 1.6: Customer Layout - Customer interface và homepage
```

#### **BƯỚC 3.2: Tasks cho Session 1.2**

**Template Prompt:**
```
"Chuyển đổi Session 1.2: Entity Models - Tạo models và migrations thành 4 tasks cụ thể:

INPUT: Session 1.2: Entity Models - Tạo models và migrations

YÊU CẦU OUTPUT (Format TODO):
- [ ] Task 1: [Action cụ thể] - [File/Location] - [Kết quả expect]
- [ ] Task 2: [Action cụ thể] - [File/Location] - [Kết quả expect]  
- [ ] Task 3: [Action cụ thể] - [File/Location] - [Kết quả expect]
- [ ] Task 4: [Action cụ thể] - [File/Location] - [Kết quả expect]

NGUYÊN TẮC:
- Tasks thực hiện trong 30-60 phút
- Ghi rõ file/folder cần tạo/sửa
- Kết quả đo được cụ thể
- Không giải thích, chỉ list tasks"
```

**Kết quả mong đợi:**
```
- [ ] Tạo User entity model - Models/User.cs - Model có properties đầy đủ với annotations
- [ ] Tạo Product/Category entities - Models/ folder - 4 entity models hoàn chỉnh
- [ ] Setup ApplicationDbContext - Data/ApplicationDbContext.cs - DbContext với DbSets configured  
- [ ] Create và run migration - Package Manager Console - Database tables tạo thành công
```

#### **BƯỚC 3.3: Validation (nếu cần)**

**Template Prompt:**
```
"Kiểm tra 4 tasks sau có đủ để hoàn thành session không:

TASKS CẦN CHECK: 
- [ ] Tạo User entity model - Models/User.cs - Model có properties đầy đủ với annotations
- [ ] Tạo Product/Category entities - Models/ folder - 4 entity models hoàn chỉnh
- [ ] Setup ApplicationDbContext - Data/ApplicationDbContext.cs - DbContext với DbSets configured  
- [ ] Create và run migration - Package Manager Console - Database tables tạo thành công

TIÊU CHÍ:
- Tasks cover đầy đủ session goal?
- Dependencies giữa tasks rõ ràng?
- Có thiếu setup/testing steps?

YÊU CẦU OUTPUT:
✅ APPROVED - Tasks đủ để hoàn thành session
❌ MISSING - [Task thiếu cần bổ sung]

Nếu MISSING, suggest thêm 1-2 tasks cần thiết"
```

**Kết quả mong đợi:**
```
✅ APPROVED - Tasks đủ để hoàn thành session
```

#### **Ví dụ nâng cao: BƯỚC 3.2 cho Phase 2**

**Session Planning cho Phase 2:**
```
"Với Phase 2: Product Management - Catalog và shopping cart - 7 sessions, chia thành sessions cụ thể:

INPUT: Phase 2: Product Management - Catalog và shopping cart - 7 sessions

YÊU CẦU OUTPUT:
Session 2.1: [Tên] - [Mục tiêu 1 dòng]
...
Session 2.7: [Tên] - [Mục tiêu 1 dòng]

NGUYÊN TẮC:
- Mỗi session 1 mục tiêu rõ ràng
- Sessions liên kết tuần tự
- Chỉ trả về danh sách sessions, không chi tiết thêm
```

**Kết quả mong đợi:**
```
Session 2.1: Category CRUD - Admin quản lý danh mục sản phẩm
Session 2.2: Product CRUD - Admin quản lý sản phẩm cơ bản
Session 2.3: Image Upload - Upload và quản lý hình ảnh sản phẩm
Session 2.4: Product Catalog - Customer xem danh sách sản phẩm
Session 2.5: Search & Filter - Tìm kiếm và lọc sản phẩm
Session 2.6: Product Detail - Trang chi tiết sản phẩm
Session 2.7: Shopping Cart - Giỏ hàng cơ bản
```

**Tasks cho Session 2.5:**
```
"Chuyển đổi Session 2.5: Search & Filter - Tìm kiếm và lọc sản phẩm thành 4 tasks cụ thể:

INPUT: Session 2.5: Search & Filter - Tìm kiếm và lọc sản phẩm

YÊU CẦU OUTPUT (Format TODO):
- [ ] Task 1: [Action cụ thể] - [File/Location] - [Kết quả expect]
- [ ] Task 2: [Action cụ thể] - [File/Location] - [Kết quả expect]  
- [ ] Task 3: [Action cụ thể] - [File/Location] - [Kết quả expect]
- [ ] Task 4: [Action cụ thể] - [File/Location] - [Kết quả expect]

NGUYÊN TẮC:
- Tasks thực hiện trong 30-60 phút
- Ghi rõ file/folder cần tạo/sửa
- Kết quả đo được cụ thể
- Không giải thích, chỉ list tasks
```

**Kết quả mong đợi:**
```
- [ ] Thêm search form - Views/Home/Index.cshtml - Form tìm kiếm có validation
- [ ] Implement search logic - Controllers/HomeController.cs - Method tìm kiếm theo tên/mô tả
- [ ] Thêm category filter - Views/Shared/_ProductFilter.cshtml - Dropdown lọc theo danh mục
- [ ] Add pagination - Views/Home/Index.cshtml - Phân trang với Previous/Next
```

---

## Phân tích code có sẵn và làm tiếp

### BƯỚC 1: Phân tích code có sẵn

#### Prompt khám phá tổng quát:
```
"Phân tích dự án này và cho tôi biết:

1. KIẾN TRÚC:
   - Framework/Technology stack đang dùng
   - Cấu trúc thư mục và tổ chức code
   - Pattern design đang follow (MVC, Repository, etc.)

2. STYLE & CONVENTION:
   - Naming convention (file, class, method)
   - Code style và formatting rules
   - CSS/styling approach đang dùng

3. TÍNH NĂNG ĐÃ CÓ:
   - Các controller/page đã implement
   - Database models và relationships
   - Authentication/authorization setup

4. DEPENDENCIES:
   - Packages/libraries đang sử dụng
   - External services integration

5. PATTERNS CẦN FOLLOW:
   - Cách viết controller actions
   - View structure và layout
   - Error handling approach
   - Validation patterns"
```

### BƯỚC 2: Hiểu design system có sẵn

#### Prompt phân tích giao diện:
```
"Xem qua các file View và CSS có sẵn, phân tích:

1. DESIGN SYSTEM:
   - Color palette đang dùng (primary, secondary, accent)
   - Typography (font families, sizes, weights)
   - Spacing system (margins, paddings)
   - Component styles (buttons, cards, forms)

2. LAYOUT STRUCTURE:
   - Header/Navigation structure
   - Main content layout pattern
   - Footer design
   - Responsive approach

3. UI COMPONENTS:
   - Button styles và variants
   - Form styling
   - Card/product display format
   - Navigation patterns

4. JAVASCRIPT/INTERACTIONS:
   - Libraries đang dùng (jQuery, Bootstrap JS, etc.)
   - Animation patterns
   - User interaction handling

Đưa ra style guide để tôi follow cho trang mới.
```

### BƯỚC 3: Hiểu backend patterns

#### Prompt phân tích API/Backend:
```
"Phân tích backend code hiện tại:

1. CONTROLLER PATTERNS:
   - Base controller structure
   - Action method naming convention
   - Response format standard
   - Error handling approach

2. DATA ACCESS:
   - ORM/Database access pattern
   - Repository pattern usage
   - Model/Entity structure

3. BUSINESS LOGIC:
   - Service layer organization
   - Validation implementation
   - Authorization/Security setup

4. API STANDARDS:
   - Route naming convention
   - HTTP status code usage
   - Request/Response DTOs
   - Error response format

Đưa ra template để tôi tạo controller/API mới theo đúng pattern.
```

### Prompt tạo trang mới theo có sẵn

#### Sau khi đã phân tích:
```
"Dựa trên phân tích code có sẵn ở trên, tạo trang [TÊN TRANG MỚI] với:

FOLLOW PATTERNS CÓ SẴN:
- Controller structure: [copy pattern từ controller hiện tại]
- View layout: [follow layout pattern đã có]
- CSS classes: [sử dụng design system đã phân tích]
- Navigation: [integrate với menu structure hiện tại]

DATABASE PATTERNS:
- ORM/Database access pattern
- Repository pattern nếu có
- Migration và seeding approach

VALIDATION:
- Model validation approach  
- Error message handling
- Custom validation attributes

AUTHENTICATION/AUTHORIZATION:
- User management system
- Role-based access patterns
- Session/token handling

Đưa ra template controller action để tôi follow.
```

### BƯỚC 4: Tạo tính năng mới với context

#### Prompt tạo tính năng với context:
```
"Dựa trên phân tích ở trên, tạo [TÊN TÍNH NĂNG] cho dự án:

CONTEXT:
- Framework: [từ phân tích]
- Patterns cần follow: [từ phân tích]
- Design system: [từ phân tích]
- Naming convention: [từ phân tích]

TÍNH NĂNG TRANG MỚI:
- [Liệt kê yêu cầu cụ thể cho trang]

ENSURE CONSISTENCY:
- Naming convention như code cũ
- Error handling giống pattern hiện tại  
- Validation style đã thiết lập
- Response format chuẩn của project

Tạo đầy đủ: Controller action, View, CSS (nếu cần), và update Navigation.
```

### Prompt mẫu cho dự án Fruitable

#### Cụ thể cho project của bạn:
```
"Phân tích dự án Fruitable ASP.NET Core này:

XEM CÁC FILE:
- Controllers (HomeController, AuthController, DashboardController)
- Views (layout structures, existing pages)
- Models (ErrorViewModel, OrderViewModel)
- CSS/styling approach
- Localization setup (en.json, vi.json)

CHO TÔI BIẾT:
1. Cấu trúc MVC pattern đang follow
2. Layout system (_Layout.cshtml, _DashboardLayout.cshtml)
3. CSS framework và custom styles
4. Authentication/Authorization setup
5. Localization implementation
6. Existing page patterns và navigation structure

Sau đó hướng dẫn tôi tạo trang mới (ví dụ: Product Management) follow đúng patterns này."
```

### Mẹo thực chiến

#### Khi hoàn toàn mù mờ:
```
"Tôi vừa nhận code dự án này và chưa hiểu gì. Hãy:

1. ĐỌC VÀ TÓM TẮT:
   - README.md hoặc documentation
   - Package.json/Project file dependencies  
   - Main entry points (Program.cs, startup files)

2. XÁC ĐỊNH:
   - Đây là dự án gì? (e-commerce, blog, CMS...)
   - Tech stack chính
   - Database setup
   - Authentication method

3. ĐƯA RA:
   - Cách run project locally
   - Cấu trúc thư mục explanation
   - Key files cần quan tâm
   - Next steps để tôi contribute"
```

#### Khi cần tạo tính năng mới:
```
"Tôi cần thêm tính năng [TÊN TÍNH NĂNG] vào dự án có sẵn này:

BƯỚC 1: Phân tích tính năng tương tự đã có
BƯỚC 2: Identify patterns và conventions cần follow  
BƯỚC 3: Plan implementation theo structure hiện tại
BƯỚC 4: Code step-by-step với consistency checks"
```

---

## Prompt cho các mô hình dự án khác nhau

### **CÔNG THỨC QUAN TRỌNG**
```
[KIẾN TRÚC DỰ ÁN] + [FRAMEWORK/TECH] + [NHIỆM VỤ CỤ THỂ] + [PATTERNS ĐẶC THÙ] + [YÊU CẦU ĐÍNH KÈM]
```

---

### MVC vs API - Cách thay đổi prompt

#### MVC TRADITIONAL (Web App với Views)
```
TEMPLATE:
"Trong dự án [TÊN] sử dụng MVC pattern:

CONTROLLER:
- Action method trả về View/PartialView
- Model binding từ form data
- ViewBag/ViewData cho dữ liệu phụ
- Redirect sau khi POST thành công

VIEW:
- Razor syntax với Model strongly-typed  
- Form helpers và validation helpers
- Layout inheritance (_Layout.cshtml)
- Partial views cho components

ROUTING:
- Convention-based: {controller}/{action}/{id}
- Route attributes nếu cần custom

YÊU CẦU: [Chi tiết tính năng]
```

**Ví dụ cụ thể:**
```
"Trong dự án Fruitable sử dụng MVC pattern:

CONTROLLER: 
- ProductController với actions Index, Details, Create, Edit, Delete
- Action Index trả về View với List<Product>
- Action Details nhận id parameter, trả về View với Product model
- POST actions có [ValidateAntiForgeryToken]

VIEW:
- Index.cshtml hiển thị danh sách products trong table/grid
- Details.cshtml hiển thị thông tin chi tiết 1 product
- Create/Edit.cshtml dùng chung partial _ProductForm.cshtml
- Sử dụng _Layout.cshtml làm master layout

ROUTING: /Product/Index, /Product/Details/5

Tạo đầy đủ CRUD cho Product management.
```

#### API ONLY (REST API)
```
TEMPLATE:
"Trong dự án [TÊN] sử dụng Web API pattern:

CONTROLLER:
- Inherit từ ControllerBase (không có View support)
- Actions trả về IActionResult/ActionResult<T>
- HTTP verbs: [HttpGet], [HttpPost], [HttpPut], [HttpDelete]
- Status codes: Ok(), BadRequest(), NotFound(), Created()

REQUEST/RESPONSE:
- Input: DTOs cho request body
- Output: DTOs cho response data  
- Consistent response wrapper: {data, message, success}
- Validation attributes trên DTOs

ROUTING:
- Attribute routing: [Route("api/[controller]")]
- RESTful conventions: GET /api/products, POST /api/products

DOCUMENTATION:
- Swagger/OpenAPI integration
- XML comments cho API docs

YÊU CẦU: [Chi tiết tính năng]
```

**Ví dụ cụ thể:**
```
"Trong dự án Fruitable API sử dụng Web API pattern:

CONTROLLER:
- ProductsController inherit ControllerBase
- GET /api/products → GetProducts() returns ActionResult<List<ProductDto>>
- GET /api/products/{id} → GetProduct(int id) returns ActionResult<ProductDto>
- POST /api/products → CreateProduct(CreateProductDto) returns CreatedAtAction
- PUT /api/products/{id} → UpdateProduct(int id, UpdateProductDto)
- DELETE /api/products/{id} → DeleteProduct(int id) returns NoContent

DTOS:
- ProductDto: Id, Name, Price, CategoryName, CreatedAt
- CreateProductDto: Name, Price, CategoryId (no Id)
- UpdateProductDto: Name, Price, CategoryId

RESPONSE FORMAT:
{
  "data": [...],
  "message": "Success",
  "success": true,
  "totalCount": 25
}

Tạo đầy đủ REST API cho Product management với Swagger docs."
```

---

### WEB vs MOBILE - Platform-specific prompts

#### WEB APPLICATION
```
TEMPLATE:
"Dự án [TÊN] là web application:

RESPONSIVE DESIGN:
- Desktop-first hoặc Mobile-first approach
- CSS Grid/Flexbox cho layout
- Media queries cho breakpoints
- Touch-friendly UI elements

BROWSER COMPATIBILITY:
- Cross-browser testing requirements
- Progressive enhancement
- Polyfills cho features mới

PERFORMANCE:
- Code splitting và lazy loading
- Image optimization và WebP support
- CSS/JS minification
- CDN integration

ACCESSIBILITY:
- ARIA labels và semantic HTML
- Keyboard navigation support
- Screen reader compatibility

SEO:
- Meta tags và structured data
- Server-side rendering (SSR)
- Sitemap và robots.txt

YÊU CẦU: [Chi tiết tính năng]"
```

**VÍ DỤ CỤ THỂ:**
```
"Dự án Fruitable là web e-commerce application:

RESPONSIVE DESIGN:
- Mobile-first approach với breakpoints: 320px, 768px, 1024px, 1200px
- CSS Grid cho product grid layout
- Flexbox cho navigation và cards
- Touch-friendly buttons minimum 44px

PERFORMANCE:
- Lazy loading cho product images
- Image optimization: WebP với JPEG fallback
- CSS critical path optimization
- JavaScript code splitting cho từng page

ACCESSIBILITY:
- Alt text cho tất cả product images
- ARIA labels cho search và cart buttons
- Color contrast ratio minimum 4.5:1
- Keyboard navigation cho tất cả interactive elements

SEO:
- Meta description cho từng product page
- Open Graph tags cho social sharing
- Structured data cho products (JSON-LD)
- Clean URLs: /products/organic-apples

Tạo responsive product listing page với performance optimization."
```

#### **MOBILE APP (Flutter)**
```
TEMPLATE:
"Dự án [TÊN] là Flutter mobile app:

ARCHITECTURE:
- BLoC/Provider/Riverpod cho state management
- Repository pattern cho data layer
- Dependency injection với get_it/provider

UI/UX:
- Material Design 3 hoặc Cupertino cho iOS
- Custom themes và color schemes
- Platform-specific adaptations
- Gesture handling và animations

NAVIGATION:
- Named routes hoặc Go Router
- Bottom navigation/Drawer cho main navigation
- Modal sheets cho secondary actions

PLATFORM FEATURES:
- Device permissions (camera, location, storage)
- Native integrations (push notifications, deep links)
- Platform channels cho native code

PERFORMANCE:
- Widget rebuilding optimization
- Image caching và lazy loading
- Memory management
- App size optimization

YÊU CẦU: [Chi tiết tính năng]"
```

**VÍ DỤ CỤ THỂ:**
```
"Dự án Fruitable là Flutter e-commerce mobile app:

ARCHITECTURE:
- BLoC pattern cho product management
- Repository layer cho API calls với Dio
- get_it cho dependency injection
- Hive cho local storage

UI/UX:
- Material Design 3 với custom green theme (#81C408)
- Custom AppBar với gradient background
- Bottom navigation: Home, Shop, Cart, Profile
- Pull-to-refresh cho product lists
- Shimmer loading effects

NAVIGATION:
- Go Router cho type-safe navigation
- Routes: /home, /shop, /product/:id, /cart
- Deep links support cho product sharing

FEATURES:
- Product grid với lazy loading
- Search với debouncing
- Add to cart animation
- Push notifications cho order updates
- Offline mode với cached data

PERFORMANCE:
- Cached network images
- ListView.builder cho large lists
- Optimized rebuilds với const constructors

Tạo product listing screen với search và filter functionality."
```

---

### **KIẾN TRÚC DỰ ÁN - Prompt templates**

#### **MONOLITHIC (All-in-one)**
```
"Dự án [TÊN] sử dụng Monolithic architecture:

STRUCTURE:
- Single deployable unit
- Shared database
- Tight coupling giữa components
- Shared dependencies

BENEFITS:
- Simple deployment
- Easy debugging
- Good cho team nhỏ
- Fast development ban đầu

CHALLENGES:
- Scaling limitations  
- Technology lock-in
- Large codebase maintenance

PROMPT FOCUS:
- Tập trung vào modularity trong code
- Clear separation of concerns
- Proper layering (Presentation, Business, Data)

YÊU CẦU: [Chi tiết tính năng]"
```

#### **MICROSERVICES**
```
"Dự án [TÊN] sử dụng Microservices architecture:

STRUCTURE:
- Multiple independent services
- Service-specific databases
- API Gateway cho routing
- Inter-service communication (HTTP/gRPC/Message queues)

PATTERNS:
- Database per service
- Circuit breaker pattern
- Event-driven architecture
- CQRS và Event Sourcing

INFRASTRUCTURE:
- Containerization (Docker/Kubernetes)
- Service discovery
- Distributed logging
- Monitoring và tracing

PROMPT FOCUS:
- Single responsibility per service
- API contract design
- Error handling across services
- Data consistency strategies

YÊU CẦU: [Chi tiết tính năng]"
```

---

### **FRAMEWORK-SPECIFIC Prompt Adjustments**

#### **ASP.NET CORE**
```
KEYWORDS THÊM VÀO:
- "Sử dụng dependency injection với built-in container"
- "Configure services trong Program.cs"  
- "Middleware pipeline cho cross-cutting concerns"
- "Entity Framework Core cho data access"
- "IConfiguration cho app settings"
- "ILogger cho structured logging"
```

#### **REACT/NEXT.JS**
```
KEYWORDS THÊM VÀO:
- "Functional components với React Hooks"
- "Custom hooks cho reusable logic"
- "Context API hoặc Redux cho state management"
- "Next.js App Router cho routing"
- "Server components và client components"
- "API routes trong /api folder"
```

#### FLUTTER
```
KEYWORDS THÊM VÀO:
- "StatefulWidget/StatelessWidget theo use case"
- "BLoC pattern với flutter_bloc package"
- "BuildContext và widget tree optimization"
- "Platform channels cho native integration"
- "pubspec.yaml cho dependencies"
- "Theme data cho consistent styling"
```

#### SPRING BOOT
```
KEYWORDS THÊM VÀO:
- "@RestController và @Service annotations"
- "Spring Security cho authentication"
- "JPA/Hibernate cho ORM"
- "application.properties/yml configuration"
- "@Autowired cho dependency injection"
- "Spring Boot Actuator cho monitoring"
```

---

## Kết luận

### **Template tổng hợp cho mọi project:**

#### BƯỚC 1: Phân tích tự động
```
"Phân tích và chia sessions cho yêu cầu:

[MÔ TẢ YÊU CẦU CỦA BẠN]

TECHNICAL CONTEXT:
- Framework: [Specify]
- Libraries: [Available tools]
- Reference: [Existing pages]
- Schema: [If applicable]

PHÂN TÍCH:
1. Workload groups
2. Todo estimation  
3. Session recommendation
4. Focus areas
5. Heavy tasks identification

OUTPUT: Session plan với format chuẩn"
```

#### BƯỚC 2: Execute sessions
Sử dụng output từ BƯỚC 1 để chạy từng session theo plan.

---

### Khi nào dùng approach nào:

**Fullstack approach khi:**
- Production application
- Cần data persistence thật
- Multi-user system
- Complex business logic
- Security requirements

**Frontend-only approach khi:**
- Prototype nhanh
- Demo cho client
- MVP testing
- Learning/practicing
- Static content sites

**API-only approach khi:**
- Mobile app backend
- Microservices architecture
- Third-party integrations
- Headless CMS
- IoT applications

---

### Lưu ý cuối cùng:

- **Luôn bắt đầu với BƯỚC 1** để có session plan tối ưu
- **SESSION 4 chỉ polish/testing** - không thêm features mới
- **Đa ngôn ngữ và animations** luôn ở sessions riêng
- **Max 4 todos/session** để đảm bảo chất lượng
- **Context và SUCCESS CRITERIA** phải rõ ràng trong mỗi session

Với những nguyên tắc này, bạn sẽ có thể:

1. **Cụ thể hóa** yêu cầu thay vì nói chung chung
2. **Thiết lập consistency** khi làm nhiều trang/API
3. **Phân tích trước** khi làm việc với code có sẵn
4. **Sử dụng template** và từ khóa mạnh

Bạn sẽ có thể tạo ra code chất lượng cao, nhất quán và dễ maintain.

---

*Tạo ngày: 9 tháng 10, 2025*  
*Cập nhật lần cuối: 11 tháng 10, 2025*
- "Theme data cho consistent styling"
```

#### **SPRING BOOT**
```
KEYWORDS THÊM VÀO:
- "@RestController và @Service annotations"
- "Spring Security cho authentication"
- "JPA/Hibernate cho ORM"
- "application.properties/yml configuration"
- "@Autowired cho dependency injection"
- "Spring Boot Actuator cho monitoring"
```

---

## **Kết luận**"
```

---

1. **Cụ thể hóa** yêu cầu thay vì nói chung chung
2. **Thiết lập consistency** khi làm nhiều trang/API
3. **Phân tích trước** khi làm việc với code có sẵn
4. **Sử dụng template** và từ khóa mạnh

Bạn sẽ có thể tạo ra code chất lượng cao, nhất quán và dễ maintain.

---

*Tạo ngày: 9 tháng 10, 2025*  
*Cập nhật lần cuối: 9 tháng 10, 2025*
