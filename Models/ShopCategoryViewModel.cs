using System.Collections.Generic;

namespace WebApp.Models
{
    public class ShopCategoryViewModel
    {
        public List<ProductItem> Products { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string? SelectedCategory { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Sort { get; set; }
    }
}
