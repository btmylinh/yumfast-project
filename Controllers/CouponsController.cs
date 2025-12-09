using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
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
            SetCouponViewBagValues();
            var coupons = GetCouponsFromDb();
            return View("~/Views/Admin/Coupons/Index.cshtml", coupons);
        }

        public IActionResult Create()
        {
            // Set coupon-specific ViewBag values
            SetCouponViewBagValues();
            
            var model = new CouponViewModel();
            return View("~/Views/Admin/Coupons/Create.cshtml", model);
        }

        [HttpPost]
        public IActionResult Create(CouponViewModel model)
        {
            if (ModelState.IsValid)
            {
                var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                using var conn = new NpgsqlConnection(config.GetConnectionString("DefaultConnection"));
                conn.Open();
                using(var dup = new NpgsqlCommand("SELECT COUNT(1) FROM coupons WHERE LOWER(code)=LOWER(@code)", conn))
                {
                    dup.Parameters.AddWithValue("@code", model.Code);
                    var exists = (long)dup.ExecuteScalar();
                    if (exists > 0)
                    {
                        ModelState.AddModelError("Code", "Mã giảm giá đã tồn tại");
                        SetCouponViewBagValues();
                        return View("~/Views/Admin/Coupons/Create.cshtml", model);
                    }
                }
                if (model.StartAt >= model.EndAt)
                {
                    ModelState.AddModelError("EndAt", "Ngày kết thúc phải sau ngày bắt đầu");
                    SetCouponViewBagValues();
                    return View("~/Views/Admin/Coupons/Create.cshtml", model);
                }
                if (model.Type == 1 && (model.Value <= 0 || model.Value > 100))
                {
                    ModelState.AddModelError("Value", "Phần trăm phải trong khoảng 1-100");
                    SetCouponViewBagValues();
                    return View("~/Views/Admin/Coupons/Create.cshtml", model);
                }
                if (model.Type == 2 && model.Value <= 0)
                {
                    ModelState.AddModelError("Value", "Giá trị phải lớn hơn 0");
                    SetCouponViewBagValues();
                    return View("~/Views/Admin/Coupons/Create.cshtml", model);
                }
                using var cmd = new NpgsqlCommand(@"INSERT INTO coupons (product_id, code, name, type, value, start_at, end_at, description, total, used_count, status)
                                                    VALUES (@pid, @code, @name, @type, @value, @start, @end, @desc, @total, 0, @status)", conn);
                cmd.Parameters.AddWithValue("@pid", (object?)model.IdProduct ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@code", model.Code);
                cmd.Parameters.AddWithValue("@name", model.Name);
                cmd.Parameters.AddWithValue("@type", (short)model.Type);
                cmd.Parameters.AddWithValue("@value", model.Value);
                cmd.Parameters.AddWithValue("@start", model.StartAt);
                cmd.Parameters.AddWithValue("@end", model.EndAt);
                cmd.Parameters.AddWithValue("@desc", (object?)model.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("@total", model.Total);
                cmd.Parameters.AddWithValue("@status", (short)model.Status);
                cmd.ExecuteNonQuery();

                TempData["Success"] = "Tạo mã giảm giá thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            // Set coupon-specific ViewBag values for re-display
            SetCouponViewBagValues();
            return View("~/Views/Admin/Coupons/Create.cshtml", model);
        }

        public IActionResult Edit(int id)
        {
            SetCouponViewBagValues();
            var coupon = GetCouponByIdFromDb(id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View("~/Views/Admin/Coupons/Edit.cshtml", coupon);
        }

        [HttpPost]
        public IActionResult Edit(CouponViewModel model)
        {
            if (ModelState.IsValid)
            {
                var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                using var conn = new NpgsqlConnection(config.GetConnectionString("DefaultConnection"));
                conn.Open();
                using(var dup = new NpgsqlCommand("SELECT COUNT(1) FROM coupons WHERE LOWER(code)=LOWER(@code) AND id<>@id", conn))
                {
                    dup.Parameters.AddWithValue("@code", model.Code);
                    dup.Parameters.AddWithValue("@id", model.Id);
                    var exists = (long)dup.ExecuteScalar();
                    if (exists > 0)
                    {
                        ModelState.AddModelError("Code", "Mã giảm giá đã tồn tại");
                        SetCouponViewBagValues();
                        return View("~/Views/Admin/Coupons/Edit.cshtml", model);
                    }
                }
                if (model.StartAt >= model.EndAt)
                {
                    ModelState.AddModelError("EndAt", "Ngày kết thúc phải sau ngày bắt đầu");
                    SetCouponViewBagValues();
                    return View("~/Views/Admin/Coupons/Edit.cshtml", model);
                }
                if (model.Type == 1 && (model.Value <= 0 || model.Value > 100))
                {
                    ModelState.AddModelError("Value", "Phần trăm phải trong khoảng 1-100");
                    SetCouponViewBagValues();
                    return View("~/Views/Admin/Coupons/Edit.cshtml", model);
                }
                if (model.Type == 2 && model.Value <= 0)
                {
                    ModelState.AddModelError("Value", "Giá trị phải lớn hơn 0");
                    SetCouponViewBagValues();
                    return View("~/Views/Admin/Coupons/Edit.cshtml", model);
                }
                using(var getUsed = new NpgsqlCommand("SELECT used_count FROM coupons WHERE id=@id", conn))
                {
                    getUsed.Parameters.AddWithValue("@id", model.Id);
                    var used = (int)(long)(getUsed.ExecuteScalar() ?? 0L);
                    if (model.Total < used)
                    {
                        ModelState.AddModelError("Total", "Tổng số lượng phải >= số đã dùng");
                        SetCouponViewBagValues();
                        return View("~/Views/Admin/Coupons/Edit.cshtml", model);
                    }
                }
                using var cmd = new NpgsqlCommand(@"UPDATE coupons SET
                                                    product_id=@pid,
                                                    code=@code,
                                                    name=@name,
                                                    type=@type,
                                                    value=@value,
                                                    start_at=@start,
                                                    end_at=@end,
                                                    description=@desc,
                                                    total=@total,
                                                    status=@status
                                                    WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@pid", (object?)model.IdProduct ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@code", model.Code);
                cmd.Parameters.AddWithValue("@name", model.Name);
                cmd.Parameters.AddWithValue("@type", (short)model.Type);
                cmd.Parameters.AddWithValue("@value", model.Value);
                cmd.Parameters.AddWithValue("@start", model.StartAt);
                cmd.Parameters.AddWithValue("@end", model.EndAt);
                cmd.Parameters.AddWithValue("@desc", (object?)model.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("@total", model.Total);
                cmd.Parameters.AddWithValue("@status", (short)model.Status);
                cmd.Parameters.AddWithValue("@id", model.Id);
                var rows = cmd.ExecuteNonQuery();
                if (rows == 0)
                {
                    return NotFound();
                }

                TempData["Success"] = "Cập nhật mã giảm giá thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            // Set coupon-specific ViewBag values for re-display
            SetCouponViewBagValues();
            return View("~/Views/Admin/Coupons/Edit.cshtml", model);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            using var conn = new NpgsqlConnection(config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM coupons WHERE id=@id AND used_count=0", conn);
            cmd.Parameters.AddWithValue("@id", id);
            var rows = cmd.ExecuteNonQuery();
            if (rows == 0)
            {
                TempData["Error"] = "Không thể xóa mã giảm giá đã được sử dụng";
            }
            else
            {
                TempData["Success"] = "Xóa mã giảm giá thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            using var conn = new NpgsqlConnection(config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new NpgsqlCommand(@"UPDATE coupons SET status = CASE WHEN status=1 THEN 0 ELSE 1 END WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            TempData["Success"] = "Thay đổi trạng thái thành công!";
            return RedirectToAction(nameof(Index));
        }

        private void SetCouponViewBagValues() { }

        private List<CouponViewModel> GetCouponsFromDb()
        {
            var list = new List<CouponViewModel>();
            var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            using var conn = new NpgsqlConnection(config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new NpgsqlCommand(@"SELECT id, product_id, code, name, type, value, start_at, end_at, description, total, used_count, status, created_at, updated_at
                                               FROM coupons
                                               ORDER BY id ASC", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new CouponViewModel
                {
                    Id = (int)r.GetInt64(0),
                    IdProduct = r.IsDBNull(1) ? null : (int?)r.GetInt64(1),
                    Code = r.GetString(2),
                    Name = r.GetString(3),
                    Type = r.GetInt16(4),
                    Value = r.GetInt32(5),
                    StartAt = r.GetDateTime(6),
                    EndAt = r.GetDateTime(7),
                    Description = r.IsDBNull(8) ? string.Empty : r.GetString(8),
                    Total = r.GetInt32(9),
                    UsedCount = r.GetInt32(10),
                    Status = r.GetInt16(11),
                    CreatedAt = r.GetDateTime(12),
                    UpdatedAt = r.GetDateTime(13)
                });
            }
            return list;
        }

        private CouponViewModel? GetCouponByIdFromDb(int id)
        {
            var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            using var conn = new NpgsqlConnection(config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new NpgsqlCommand(@"SELECT id, product_id, code, name, type, value, start_at, end_at, description, total, used_count, status, created_at, updated_at
                                               FROM coupons WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new CouponViewModel
            {
                Id = (int)r.GetInt64(0),
                IdProduct = r.IsDBNull(1) ? null : (int?)r.GetInt64(1),
                Code = r.GetString(2),
                Name = r.GetString(3),
                Type = r.GetInt16(4),
                Value = r.GetInt32(5),
                StartAt = r.GetDateTime(6),
                EndAt = r.GetDateTime(7),
                Description = r.IsDBNull(8) ? string.Empty : r.GetString(8),
                Total = r.GetInt32(9),
                UsedCount = r.GetInt32(10),
                Status = r.GetInt16(11),
                CreatedAt = r.GetDateTime(12),
                UpdatedAt = r.GetDateTime(13)
            };
        }
    }
}
