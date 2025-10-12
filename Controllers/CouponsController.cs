using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers
{
    public class CouponsController : BaseController
    {
        public CouponsController(IJsonLocalizationService localizationService) : base(localizationService)
        {
        }

        public IActionResult Index()
        {
            // Set coupon-specific ViewBag values
            SetCouponViewBagValues();
            
            var coupons = GetMockCoupons();
            return View(coupons);
        }

        public IActionResult Create()
        {
            // Set coupon-specific ViewBag values
            SetCouponViewBagValues();
            
            var model = new CouponViewModel();
            return View(model);
        }

        [HttpPost]
        public IActionResult Create(CouponViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Mock create logic - trong thực tế sẽ lưu vào database
                TempData["Success"] = "Tạo mã giảm giá thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            // Set coupon-specific ViewBag values for re-display
            SetCouponViewBagValues();
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            // Set coupon-specific ViewBag values
            SetCouponViewBagValues();
            
            var coupon = GetMockCoupons().FirstOrDefault(c => c.Id == id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }

        [HttpPost]
        public IActionResult Edit(CouponViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Mock edit logic - trong thực tế sẽ cập nhật database
                TempData["Success"] = "Cập nhật mã giảm giá thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            // Set coupon-specific ViewBag values for re-display
            SetCouponViewBagValues();
            return View(model);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            // Mock delete logic - trong thực tế sẽ xóa khỏi database
            TempData["Success"] = "Xóa mã giảm giá thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            // Mock toggle status logic
            TempData["Success"] = "Thay đổi trạng thái thành công!";
            return RedirectToAction(nameof(Index));
        }

        private void SetCouponViewBagValues()
        {
            // Coupon page titles and labels
            ViewBag.CouponTitle = _localizationService.GetLocalizedString("couponTitle");
            ViewBag.CouponAddNew = _localizationService.GetLocalizedString("couponAddNew");
            ViewBag.CouponEdit = _localizationService.GetLocalizedString("couponEdit");
            ViewBag.CouponBackToList = _localizationService.GetLocalizedString("couponBackToList");
            ViewBag.CouponSearch = _localizationService.GetLocalizedString("couponSearch");
            
            // Table headers
            ViewBag.CouponCode = _localizationService.GetLocalizedString("couponCode");
            ViewBag.CouponName = _localizationService.GetLocalizedString("couponName");
            ViewBag.CouponType = _localizationService.GetLocalizedString("couponType");
            ViewBag.CouponValue = _localizationService.GetLocalizedString("couponValue");
            ViewBag.CouponPeriod = _localizationService.GetLocalizedString("couponPeriod");
            ViewBag.CouponUsage = _localizationService.GetLocalizedString("couponUsage");
            ViewBag.CouponStatus = _localizationService.GetLocalizedString("couponStatus");
            
            // Status values
            ViewBag.CouponStart = _localizationService.GetLocalizedString("couponStart");
            ViewBag.CouponEnd = _localizationService.GetLocalizedString("couponEnd");
            ViewBag.CouponRemaining = _localizationService.GetLocalizedString("couponRemaining");
            ViewBag.CouponActive = _localizationService.GetLocalizedString("couponActive");
            ViewBag.CouponInactive = _localizationService.GetLocalizedString("couponInactive");
            ViewBag.CouponPending = _localizationService.GetLocalizedString("couponPending");
            ViewBag.CouponExpired = _localizationService.GetLocalizedString("couponExpired");
            
            // Actions
            ViewBag.CouponActivate = _localizationService.GetLocalizedString("couponActivate");
            ViewBag.CouponDeactivate = _localizationService.GetLocalizedString("couponDeactivate");
            ViewBag.CouponDelete = _localizationService.GetLocalizedString("couponDelete");
            ViewBag.CouponDeleteConfirm = _localizationService.GetLocalizedString("couponDeleteConfirm");
            ViewBag.CouponNoData = _localizationService.GetLocalizedString("couponNoData");
            
            // Form fields
            ViewBag.CouponBasicInfo = _localizationService.GetLocalizedString("couponBasicInfo");
            ViewBag.CouponSettings = _localizationService.GetLocalizedString("couponSettings");
            ViewBag.CouponPreview = _localizationService.GetLocalizedString("couponPreview");
            ViewBag.CouponCodePlaceholder = _localizationService.GetLocalizedString("couponCodePlaceholder");
            ViewBag.CouponNamePlaceholder = _localizationService.GetLocalizedString("couponNamePlaceholder");
            ViewBag.CouponSelectType = _localizationService.GetLocalizedString("couponSelectType");
            ViewBag.CouponPercentage = _localizationService.GetLocalizedString("couponPercentage");
            ViewBag.CouponFixedAmount = _localizationService.GetLocalizedString("couponFixedAmount");
            ViewBag.CouponValuePlaceholder = _localizationService.GetLocalizedString("couponValuePlaceholder");
            ViewBag.CouponValueUnit = _localizationService.GetLocalizedString("couponValueUnit");
            ViewBag.CouponStartDate = _localizationService.GetLocalizedString("couponStartDate");
            ViewBag.CouponEndDate = _localizationService.GetLocalizedString("couponEndDate");
            ViewBag.CouponTotalQuantity = _localizationService.GetLocalizedString("couponTotalQuantity");
            ViewBag.CouponTotalPlaceholder = _localizationService.GetLocalizedString("couponTotalPlaceholder");
            ViewBag.CouponProductOptional = _localizationService.GetLocalizedString("couponProductOptional");
            ViewBag.CouponAllProducts = _localizationService.GetLocalizedString("couponAllProducts");
            ViewBag.CouponProductNote = _localizationService.GetLocalizedString("couponProductNote");
            ViewBag.CouponDescription = _localizationService.GetLocalizedString("couponDescription");
            ViewBag.CouponDescriptionPlaceholder = _localizationService.GetLocalizedString("couponDescriptionPlaceholder");
            
            // Buttons
            ViewBag.CouponSave = _localizationService.GetLocalizedString("couponSave");
            ViewBag.CouponUpdate = _localizationService.GetLocalizedString("couponUpdate");
            ViewBag.CouponCancel = _localizationService.GetLocalizedString("couponCancel");
            
            // Preview labels
            ViewBag.CouponCodePreview = _localizationService.GetLocalizedString("couponCodePreview");
            ViewBag.CouponNamePreview = _localizationService.GetLocalizedString("couponNamePreview");
            ViewBag.CouponValuePreview = _localizationService.GetLocalizedString("couponValuePreview");
            ViewBag.CouponTypePreview = _localizationService.GetLocalizedString("couponTypePreview");
            ViewBag.CouponPeriodPreview = _localizationService.GetLocalizedString("couponPeriodPreview");
            
            // Statistics
            ViewBag.CouponUsageStats = _localizationService.GetLocalizedString("couponUsageStats");
            ViewBag.CouponUsed = _localizationService.GetLocalizedString("couponUsed");
            ViewBag.CouponCreatedAt = _localizationService.GetLocalizedString("couponCreatedAt");
            ViewBag.CouponUpdatedAt = _localizationService.GetLocalizedString("couponUpdatedAt");
            ViewBag.CouponCurrentStatus = _localizationService.GetLocalizedString("couponCurrentStatus");
            
            // Validation messages
            ViewBag.CouponDateError = _localizationService.GetLocalizedString("couponDateError");
            ViewBag.CouponPercentageError = _localizationService.GetLocalizedString("couponPercentageError");
            ViewBag.CouponAmountError = _localizationService.GetLocalizedString("couponAmountError");
            ViewBag.CouponTotalError = _localizationService.GetLocalizedString("couponTotalError");
        }

        private List<CouponViewModel> GetMockCoupons()
        {
            return new List<CouponViewModel>
            {
                new CouponViewModel
                {
                    Id = 1,
                    Code = "SUMMER2024",
                    Name = "Giảm giá mùa hè 2024",
                    Type = 1, // %
                    Value = 15,
                    StartAt = DateTime.Now.AddDays(-10),
                    EndAt = DateTime.Now.AddDays(20),
                    Description = "Áp dụng cho tất cả sản phẩm trái cây tươi",
                    Total = 100,
                    UsedCount = 25,
                    Status = 1,
                    CreatedAt = DateTime.Now.AddDays(-15),
                    UpdatedAt = DateTime.Now.AddDays(-1)
                },
                new CouponViewModel
                {
                    Id = 2,
                    Code = "NEWUSER50",
                    Name = "Ưu đãi khách hàng mới",
                    Type = 2, // tiền cứng
                    Value = 50000,
                    StartAt = DateTime.Now.AddDays(-5),
                    EndAt = DateTime.Now.AddDays(30),
                    Description = "Dành cho khách hàng đăng ký lần đầu",
                    Total = 500,
                    UsedCount = 127,
                    Status = 1,
                    CreatedAt = DateTime.Now.AddDays(-7),
                    UpdatedAt = DateTime.Now.AddDays(-2)
                },
                new CouponViewModel
                {
                    Id = 3,
                    Code = "FRUIT20",
                    Name = "Giảm 20% trái cây",
                    Type = 1, // %
                    Value = 20,
                    StartAt = DateTime.Now.AddDays(-30),
                    EndAt = DateTime.Now.AddDays(-5),
                    Description = "Chuyên dụng cho danh mục trái cây",
                    Total = 200,
                    UsedCount = 198,
                    Status = 0, // Đã hết hạn
                    IdProduct = 1,
                    CreatedAt = DateTime.Now.AddDays(-35),
                    UpdatedAt = DateTime.Now.AddDays(-5)
                },
                new CouponViewModel
                {
                    Id = 4,
                    Code = "WEEKEND100",
                    Name = "Cuối tuần giảm 100k",
                    Type = 2, // tiền cứng
                    Value = 100000,
                    StartAt = DateTime.Now.AddDays(5),
                    EndAt = DateTime.Now.AddDays(15),
                    Description = "Áp dụng cho đơn hàng từ 500k trở lên",
                    Total = 50,
                    UsedCount = 0,
                    Status = 1,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new CouponViewModel
                {
                    Id = 5,
                    Code = "ORGANIC30",
                    Name = "Giảm 30% thực phẩm hữu cơ",
                    Type = 1, // %
                    Value = 30,
                    StartAt = DateTime.Now.AddDays(-2),
                    EndAt = DateTime.Now.AddDays(25),
                    Description = "Dành riêng cho sản phẩm hữu cơ",
                    Total = 75,
                    UsedCount = 12,
                    Status = 1,
                    CreatedAt = DateTime.Now.AddDays(-3),
                    UpdatedAt = DateTime.Now
                }
            };
        }
    }
}
