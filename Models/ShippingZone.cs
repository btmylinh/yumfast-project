namespace WebApp.Models
{
    public class ShippingZone
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty; // "NOI_THANH", "NGOAI_THANH"
        public string Name { get; set; } = string.Empty; // "Nội thành", "Ngoài thành"
        public int BaseFee { get; set; } = 0; // Phí cơ bản (VND)
        public int FreeMinimum { get; set; } = 0; // Miễn phí nếu >= số tiền này
        public short Status { get; set; } = 1; // 0=inactive, 1=active
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    }
}

