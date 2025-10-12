using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Service for banner business logic and data access
    /// </summary>
    public interface IBannerService
    {
        Task<IEnumerable<Banner>> GetBannersAsync(string? search = null, int? status = null, int page = 1, int pageSize = 10, string? sortBy = "CreatedAt", string? sortDirection = "desc");
        Task<Banner?> GetBannerByIdAsync(long id);
        Task<Banner> CreateBannerAsync(BannerCreateRequest request);
        Task<Banner?> UpdateBannerAsync(long id, BannerUpdateRequest request);
        Task<bool> DeleteBannerAsync(long id);
        Task<Banner?> ToggleBannerStatusAsync(long id);
        Task<int> GetBannerCountAsync(string? search = null, int? status = null);
    }

    public class BannerService : IBannerService
    {
        private readonly ApplicationDbContext _context;

        public BannerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Banner>> GetBannersAsync(string? search = null, int? status = null, int page = 1, int pageSize = 10, string? sortBy = "CreatedAt", string? sortDirection = "desc")
        {
            var query = _context.Banners.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(b => b.Name.ToLower().Contains(searchLower) || 
                                       (b.Link != null && b.Link.ToLower().Contains(searchLower)));
            }

            // Apply status filter
            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "name" => sortDirection.ToLower() == "asc" 
                    ? query.OrderBy(b => b.Name) 
                    : query.OrderByDescending(b => b.Name),
                "status" => sortDirection.ToLower() == "asc" 
                    ? query.OrderBy(b => b.Status) 
                    : query.OrderByDescending(b => b.Status),
                "createdat" => sortDirection.ToLower() == "asc" 
                    ? query.OrderBy(b => b.CreatedAt) 
                    : query.OrderByDescending(b => b.CreatedAt),
                _ => sortDirection.ToLower() == "asc" 
                    ? query.OrderBy(b => b.CreatedAt) 
                    : query.OrderByDescending(b => b.CreatedAt)
            };

            // Apply pagination
            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Banner?> GetBannerByIdAsync(long id)
        {
            return await _context.Banners
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Banner> CreateBannerAsync(BannerCreateRequest request)
        {
            var banner = new Banner
            {
                Name = request.Name,
                Image = request.Image,
                Link = request.Link,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Banners.Add(banner);
            await _context.SaveChangesAsync();
            return banner;
        }

        public async Task<Banner?> UpdateBannerAsync(long id, BannerUpdateRequest request)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
                return null;

            banner.Name = request.Name;
            banner.Image = request.Image;
            banner.Link = request.Link;
            banner.Status = request.Status;
            banner.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return banner;
        }

        public async Task<bool> DeleteBannerAsync(long id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
                return false;

            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Banner?> ToggleBannerStatusAsync(long id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
                return null;

            banner.Status = banner.Status == 1 ? (short)0 : (short)1;
            banner.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return banner;
        }

        public async Task<int> GetBannerCountAsync(string? search = null, int? status = null)
        {
            var query = _context.Banners.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(b => b.Name.ToLower().Contains(searchLower) || 
                                       (b.Link != null && b.Link.ToLower().Contains(searchLower)));
            }

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            return await query.CountAsync();
        }
    }
}
