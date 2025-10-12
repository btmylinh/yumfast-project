using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class CouponViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Product ID")]
        public int? IdProduct { get; set; }

        [Required(ErrorMessage = "Mã giảm giá là bắt buộc")]
        [StringLength(30, ErrorMessage = "Mã giảm giá không được vượt quá 30 ký tự")]
        [Display(Name = "Mã giảm giá")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên mã giảm giá là bắt buộc")]
        [StringLength(150, ErrorMessage = "Tên không được vượt quá 150 ký tự")]
        [Display(Name = "Tên mã giảm giá")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc")]
        [Display(Name = "Loại giảm giá")]
        public int Type { get; set; } // 1: %, 2: tiền cứng

        [Required(ErrorMessage = "Giá trị là bắt buộc")]
        [Display(Name = "Giá trị")]
        public int Value { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [Display(Name = "Ngày bắt đầu")]
        public DateTime StartAt { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        [Display(Name = "Ngày kết thúc")]
        public DateTime EndAt { get; set; }

        [Display(Name = "Mô tả cách sử dụng")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Tổng số lượng là bắt buộc")]
        [Display(Name = "Tổng số lượng")]
        public int Total { get; set; }

        [Display(Name = "Số lượng đã sử dụng")]
        public int UsedCount { get; set; } = 0;

        [Display(Name = "Trạng thái")]
        public int Status { get; set; } = 1; // 1: Hoạt động, 0: Không hoạt động

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ngày cập nhật")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Helper properties
        public string TypeDisplay => Type == 1 ? "%" : "VNĐ";
        public string StatusDisplay => Status == 1 ? "Hoạt động" : "Không hoạt động";
        public bool IsActive => Status == 1 && DateTime.Now >= StartAt && DateTime.Now <= EndAt;
        public int RemainingCount => Total - UsedCount;
        public string FormattedValue => Type == 1 ? $"{Value}%" : $"{Value:N0} VNĐ";
    }
}
