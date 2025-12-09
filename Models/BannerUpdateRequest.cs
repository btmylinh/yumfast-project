using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class BannerUpdateRequest
    {
        [Required(ErrorMessage = "Banner name is required")]
        [StringLength(120, MinimumLength = 2, ErrorMessage = "Banner name must be between 2 and 120 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-_.,!?()àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđĐÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ]+$", ErrorMessage = "Banner name contains invalid characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Banner image is required")]
        [StringLength(255, ErrorMessage = "Banner image path cannot exceed 255 characters")]
        [RegularExpression(@"^(https?://.*\.(jpg|jpeg|png|gif|webp)(\?.*)?|/assets/images/.+\.(jpg|jpeg|png|gif|webp))$", ErrorMessage = "Image must be a valid http(s) URL or /assets/images/... path")]
        public string Image { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Banner link cannot exceed 255 characters")]
        [RegularExpression(@"^(https?://.+|/[-a-zA-Z0-9/_?&=.#]+)$", ErrorMessage = "Please provide a valid link URL or relative path")]
        public string? Link { get; set; }

        [Range(0, 1, ErrorMessage = "Status must be 0 (inactive) or 1 (active)")]
        public short Status { get; set; } = 1;
    }
}
