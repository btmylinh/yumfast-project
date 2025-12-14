using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/coupons")] // Giữ nguyên route API Public
    public class CouponsController : BaseController
    {
        private readonly IConfiguration _config;

        public CouponsController(IJsonLocalizationService localizationService, IConfiguration config)
            : base(localizationService)
        {
            _config = config;
        }

        // ============================================================
        // ============= 🔵 NHÓM PUBLIC API (GET /api/coupons) =========
        // ============================================================

        // API lấy danh sách mã giảm giá đang hoạt động
        // GET /api/coupons/active
        [HttpGet("active")]
        public IActionResult GetActive([FromQuery] long? productId)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var where = "WHERE status=1 AND start_at <= NOW() AND end_at > NOW() AND used_count < total";
            if (productId.HasValue)
                where += " AND (product_id IS NULL OR product_id=@pid)";

            using var cmd = new NpgsqlCommand($@"
                SELECT id, code, name, type, value, start_at, end_at,
                       description, total, used_count, product_id
                FROM coupons
                {where}
                ORDER BY start_at DESC", conn);

            if (productId.HasValue)
                cmd.Parameters.AddWithValue("@pid", productId.Value);

            var list = new List<CouponDto>();
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                var total = r.GetInt32(8);
                var used = r.GetInt32(9);

                list.Add(new CouponDto
                {
                    Id = r.GetInt64(0),
                    Code = r.GetString(1),
                    Name = r.GetString(2),
                    Type = r.GetInt16(3),
                    Value = r.GetInt32(4),
                    StartAt = r.GetDateTime(5),
                    EndAt = r.GetDateTime(6),
                    Description = r.IsDBNull(7) ? "" : r.GetString(7),
                    Total = total,
                    UsedCount = used,
                    Remaining = total - used,
                    ProductId = r.IsDBNull(10) ? null : r.GetInt64(10)
                });
            }

            return Ok(new { data = list });
        }


        // ============================================================
        // ============= 🔵 NHÓM MVC ADMIN (Trang quản trị) ============
        // ============================================================

        // Trang danh sách coupon (Admin)
        [Route("~/coupons")]
        [Route("~/coupons/index")]
        public IActionResult Index()
        {
            SetCouponViewBagValues();
            var coupons = GetCouponsFromDb();
            return View("~/Views/Admin/Coupons/Index.cshtml", coupons);
        }

        // Trang tạo coupon
        [HttpGet("~/coupons/create")]
        public IActionResult Create()
        {
            SetCouponViewBagValues();
            return View("~/Views/Admin/Coupons/Create.cshtml", new CouponViewModel());
        }

        // Xử lý tạo coupon
        [HttpPost("~/coupons/create")]
        public IActionResult Create(CouponViewModel model)
        {
            if (ModelState.IsValid)
            {
                using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Check mã trùng
                using (var dup = new NpgsqlCommand("SELECT COUNT(1) FROM coupons WHERE LOWER(code)=LOWER(@code)", conn))
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

                // Validate ngày tháng
                if (model.StartAt >= model.EndAt)
                {
                    ModelState.AddModelError("EndAt", "Ngày kết thúc phải sau ngày bắt đầu");
                    SetCouponViewBagValues();
                    return View("~/Views/Admin/Coupons/Create.cshtml", model);
                }

                // Validate % giảm giá
                if (model.Type == 1 && (model.Value <= 0 || model.Value > 100))
                {
                    ModelState.AddModelError("Value", "Phần trăm phải trong khoảng 1 - 100");
                    SetCouponViewBagValues();
                    return View("~/Views/Admin/Coupons/Create.cshtml", model);
                }

                // Validate giảm tiền
                if (model.Type == 2 && model.Value <= 0)
                {
                    ModelState.AddModelError("Value", "Giá trị phải lớn hơn 0");
                    SetCouponViewBagValues();
                    return View("~/Views/Admin/Coupons/Create.cshtml", model);
                }

                // Insert coupon
                using var cmd = new NpgsqlCommand(@"
                    INSERT INTO coupons
                    (product_id, code, name, type, value, start_at, end_at, description, total, used_count, status)
                    VALUES (@pid, @code, @name, @type, @value, @start, @end, @desc, @total, 0, @status)
                ", conn);

                cmd.Parameters.AddWithValue("@pid", (object?)model.IdProduct ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@code", model.Code);
                cmd.Parameters.AddWithValue("@name", model.Name);
                cmd.Parameters.AddWithValue("@type", (short)model.Type);
                cmd.Parameters.AddWithValue("@value", model.Value);
                cmd.Parameters.AddWithValue("@start", model.StartAt);
                cmd.Parameters.AddWithValue("@end", model.EndAt);
                cmd.Parameters.AddWithValue("@desc", (object?)model.Description ?? "");
                cmd.Parameters.AddWithValue("@total", model.Total);
                cmd.Parameters.AddWithValue("@status", (short)model.Status);

                cmd.ExecuteNonQuery();

                TempData["Success"] = "Tạo mã giảm giá thành công!";
                return RedirectToAction(nameof(Index));
            }

            SetCouponViewBagValues();
            return View("~/Views/Admin/Coupons/Create.cshtml", model);
        }

        // Trang sửa coupon
        [HttpGet("~/coupons/edit/{id}")]
        public IActionResult Edit(int id)
        {
            SetCouponViewBagValues();
            var coupon = GetCouponByIdFromDb(id);

            if (coupon == null)
                return NotFound();

            return View("~/Views/Admin/Coupons/Edit.cshtml", coupon);
        }

        // Xử lý sửa coupon
        [HttpPost("~/coupons/edit")]
        public IActionResult Edit(CouponViewModel model)
        {
            if (ModelState.IsValid)
            {
                using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Check mã trùng
                using (var dup = new NpgsqlCommand("SELECT COUNT(1) FROM coupons WHERE LOWER(code)=LOWER(@code) AND id<>@id", conn))
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

                // Validate ngày tháng
                if (model.StartAt >= model.EndAt)
                {
                    ModelState.AddModelError("EndAt", "Ngày kết thúc phải sau ngày bắt đầu");
                    SetCouponViewBagValues();
                    return View("~/Views/Admin/Coupons/Edit.cshtml", model);
                }

                // Validate %
                if (model.Type == 1 && (model.Value <= 0 || model.Value > 100))
                {
                    ModelState.AddModelError("Value", "Phần trăm phải trong khoảng 1 - 100");
                    SetCouponViewBagValues();
                    return View("~/Views/Admin/Coupons/Edit.cshtml", model);
                }

                // Validate số lượng không < used_count
                using (var getUsed = new NpgsqlCommand("SELECT used_count FROM coupons WHERE id=@id", conn))
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

                // Update coupon
                using var cmd = new NpgsqlCommand(@"
                    UPDATE coupons SET
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
                    WHERE id=@id
                ", conn);

                cmd.Parameters.AddWithValue("@pid", (object?)model.IdProduct ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@code", model.Code);
                cmd.Parameters.AddWithValue("@name", model.Name);
                cmd.Parameters.AddWithValue("@type", (short)model.Type);
                cmd.Parameters.AddWithValue("@value", model.Value);
                cmd.Parameters.AddWithValue("@start", model.StartAt);
                cmd.Parameters.AddWithValue("@end", model.EndAt);
                cmd.Parameters.AddWithValue("@desc", (object?)model.Description ?? "");
                cmd.Parameters.AddWithValue("@total", model.Total);
                cmd.Parameters.AddWithValue("@status", (short)model.Status);
                cmd.Parameters.AddWithValue("@id", model.Id);

                var rows = cmd.ExecuteNonQuery();

                if (rows == 0)
                    return NotFound();

                TempData["Success"] = "Cập nhật mã giảm giá thành công!";
                return RedirectToAction(nameof(Index));
            }

            SetCouponViewBagValues();
            return View("~/Views/Admin/Coupons/Edit.cshtml", model);
        }

        // Xóa mã giảm giá
        [HttpPost("~/coupons/delete")]
        public IActionResult Delete(int id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand("DELETE FROM coupons WHERE id=@id AND used_count=0", conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();

            if (rows == 0)
                TempData["Error"] = "Không thể xóa mã giảm giá đã được sử dụng";
            else
                TempData["Success"] = "Xóa mã giảm giá thành công!";

            return RedirectToAction(nameof(Index));
        }

        // Toggle trạng thái
        [HttpPost("~/coupons/togglestatus")]
        public IActionResult ToggleStatus(int id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "UPDATE coupons SET status = CASE WHEN status=1 THEN 0 ELSE 1 END WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            TempData["Success"] = "Thay đổi trạng thái thành công!";
            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // ============= 🔵 PUBLIC VIEW PAGE (THAY CHO CouponViewController)
        // ============================================================

        // Trang hiển thị coupon phía người dùng
        [Route("~/coupons/view")]
        public IActionResult ViewCoupons()
        {
            return base.View("~/Views/Coupons/Index.cshtml");
        }


        // ============================================================
        // ============= 🔧 HÀM PHỤ TRỢ =================================
        // ============================================================

        private void SetCouponViewBagValues()
        {
            // Future use - giữ nguyên logic cũ
        }

        private List<CouponViewModel> GetCouponsFromDb()
        {
            var list = new List<CouponViewModel>();

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT id, product_id, code, name, type, value,
                       start_at, end_at, description, total,
                       used_count, status, created_at, updated_at
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
                    Description = r.IsDBNull(8) ? "" : r.GetString(8),
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
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT id, product_id, code, name, type, value,
                       start_at, end_at, description, total,
                       used_count, status, created_at, updated_at
                FROM coupons
                WHERE id=@id", conn);

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
                Description = r.IsDBNull(8) ? "" : r.GetString(8),
                Total = r.GetInt32(9),
                UsedCount = r.GetInt32(10),
                Status = r.GetInt16(11),
                CreatedAt = r.GetDateTime(12),
                UpdatedAt = r.GetDateTime(13)
            };
        }
    }


    // ============================================================
    // ============= DTO cho API Public ============================
    // ============================================================

    public class CouponDto
    {
        public long Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public short Type { get; set; }
        public int Value { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Description { get; set; } = "";
        public int Total { get; set; }
        public int UsedCount { get; set; }
        public int Remaining { get; set; }
        public long? ProductId { get; set; }
    }
}
