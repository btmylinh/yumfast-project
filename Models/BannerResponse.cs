namespace WebApp.Models
{
    /// <summary>
    /// Response model for paginated banner list
    /// </summary>
    public class BannerListResponse
    {
        /// <summary>
        /// List of banners
        /// </summary>
        public IEnumerable<Banner> Data { get; set; } = new List<Banner>();

        /// <summary>
        /// Total number of banners
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Current page number
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Response model for API operations
    /// </summary>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Success status
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Response message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Response data
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Error details (if any)
        /// </summary>
        public string? Error { get; set; }
    }
}
