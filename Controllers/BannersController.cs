using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.Text;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Temporarily disabled for testing
    public class BannersController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BannersController(ApplicationDbContext context, IJsonLocalizationService localizationService, IWebHostEnvironment env) 
            : base(localizationService)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        /// Get all banners with optional search, filter, pagination and sorting
        /// </summary>
        /// <param name="search">Search term for banner name or link</param>
        /// <param name="status">Filter by status (1=active, 0=inactive)</param>
        /// <param name="page">Page number for pagination</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="sortBy">Field to sort by (name, status, createdAt)</param>
        /// <param name="sortDirection">Sort direction (asc, desc)</param>
        /// <returns>Paginated list of banners</returns>
        // GET: api/banners
        [HttpGet]
        [AllowAnonymous] // Allow public access to view banners
        public async Task<ActionResult<IEnumerable<Banner>>> GetBanners(
            [FromQuery] string? search = null,
            [FromQuery] int? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "CreatedAt",
            [FromQuery] string? sortDirection = "desc")
        {
            try
            {
                var query = _context.Banners.AsQueryable();

                // Search filter with case-insensitive search
                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(b => b.Name.ToLower().Contains(searchLower) || 
                                           (b.Link != null && b.Link.ToLower().Contains(searchLower)));
                }

                // Status filter
                if (status.HasValue)
                {
                    query = query.Where(b => b.Status == status.Value);
                }

                // Sorting
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

                // Get total count before pagination
                var totalCount = await query.CountAsync();
                
                // Apply pagination
                var banners = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking() // Improve performance for read-only operations
                    .ToListAsync();

                return Ok(new
                {
                    data = banners,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = "internal_server_error", error = ex.Message });
            }
        }

        [HttpPost("upload")]
        public IActionResult Upload()
        {
            var files = Request.Form.Files;
            if (files == null || files.Count == 0) return BadRequest(new { message = "no_files" });
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var target = System.IO.Path.Combine(_env.WebRootPath, "assets", "images", "banner");
            if (!System.IO.Directory.Exists(target)) System.IO.Directory.CreateDirectory(target);
            var saved = new List<string>();
            foreach (var f in files)
            {
                var ext = System.IO.Path.GetExtension(f.FileName);
                if (string.IsNullOrWhiteSpace(ext) || !allowed.Contains(ext)) return BadRequest(new { message = "invalid_extension" });
                if (f.Length <= 0 || f.Length > 5 * 1024 * 1024) return BadRequest(new { message = "invalid_size" });
                var baseName = NormalizeSlug(System.IO.Path.GetFileNameWithoutExtension(f.FileName));
                var fileName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{baseName}{ext.ToLowerInvariant()}";
                var path = System.IO.Path.Combine(target, fileName);
                using (var s = System.IO.File.Create(path)) { f.CopyTo(s); }
                saved.Add(fileName);
            }
            return Ok(new { files = saved });
        }

        /// <summary>
        /// Get a specific banner by ID
        /// </summary>
        /// <param name="id">Banner ID</param>
        /// <returns>Banner details</returns>
        // GET: api/banners/5
        [HttpGet("{id}")]
        [AllowAnonymous] // Allow public access to view individual banner
        public async Task<ActionResult<Banner>> GetBanner(long id)
        {
            try
            {
                var banner = await _context.Banners
                    .AsNoTracking() // Improve performance for read-only operations
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (banner == null)
                {
                    return NotFound(new { code = "banner_not_found" });
                }

                return Ok(banner);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = "internal_server_error", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new banner
        /// </summary>
        /// <param name="request">Banner creation data</param>
        /// <returns>Created banner</returns>
        // POST: api/banners
        [HttpPost]
        public async Task<ActionResult<Banner>> CreateBanner([FromBody] BannerCreateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { 
                        code = "validation_failed", 
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) 
                    });
                }

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

                return CreatedAtAction(nameof(GetBanner), new { id = banner.Id }, banner);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = "internal_server_error", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing banner
        /// </summary>
        /// <param name="id">Banner ID</param>
        /// <param name="request">Banner update data</param>
        /// <returns>Updated banner</returns>
        // PUT: api/banners/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBanner(long id, [FromBody] BannerUpdateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { 
                        code = "validation_failed", 
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) 
                    });
                }

                var existingBanner = await _context.Banners.FindAsync(id);
                if (existingBanner == null)
                {
                    return NotFound(new { code = "banner_not_found" });
                }

                existingBanner.Name = request.Name;
                existingBanner.Image = request.Image;
                existingBanner.Link = request.Link;
                existingBanner.Status = request.Status;
                existingBanner.UpdatedAt = DateTime.UtcNow;

                _context.Entry(existingBanner).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { code = "banner_updated", data = existingBanner });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BannerExists(id))
                {
                    return NotFound(new { code = "banner_not_found" });
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = "internal_server_error", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a banner
        /// </summary>
        /// <param name="id">Banner ID</param>
        /// <returns>Success message</returns>
        // DELETE: api/banners/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBanner(long id)
        {
            try
            {
                var banner = await _context.Banners.FindAsync(id);
                if (banner == null)
                {
                    return NotFound(new { code = "banner_not_found" });
                }

                _context.Banners.Remove(banner);
                await _context.SaveChangesAsync();

                return Ok(new { code = "banner_deleted" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = "internal_server_error", error = ex.Message });
            }
        }

        /// <summary>
        /// Toggle banner status (active/inactive)
        /// </summary>
        /// <param name="id">Banner ID</param>
        /// <returns>Updated banner with new status</returns>
        // PATCH: api/banners/5/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleBannerStatus(long id)
        {
            try
            {
                var banner = await _context.Banners.FindAsync(id);
                if (banner == null)
                {
                    return NotFound(new { code = "banner_not_found" });
                }

                banner.Status = banner.Status == 1 ? (short)0 : (short)1;
                banner.UpdatedAt = DateTime.UtcNow;

                _context.Entry(banner).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { 
                    code = banner.Status == 1 ? "banner_activated" : "banner_deactivated", 
                    data = banner 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = "internal_server_error", error = ex.Message });
            }
        }

        private bool BannerExists(long id)
        {
            return _context.Banners.Any(e => e.Id == id);
        }

        private string NormalizeSlug(string s)
        {
            s = (s ?? string.Empty).Trim().ToLowerInvariant();
            var normalized = s.Normalize(NormalizationForm.FormD);
            var chars = normalized.Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark);
            var filtered = new string(chars.ToArray());
            var sb = new System.Text.StringBuilder();
            foreach (var ch in filtered)
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == ' ')
                    sb.Append(ch);
            }
            var result = sb.ToString().Replace(' ', '-');
            while (result.Contains("--")) result = result.Replace("--", "-");
            return result.Trim('-');
        }
    }
}
