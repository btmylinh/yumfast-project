using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoriesService _service;

        public CategoriesController(ICategoriesService service)
        {
            _service = service;
        }

        // ---------------------------------------------------------
        // GET /api/categories
        // ---------------------------------------------------------
        [HttpGet]
        public IActionResult Get(
            [FromQuery] string? search,
            [FromQuery] int? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "created_at",
            [FromQuery] string? sortDirection = "desc")
        {
            var result = _service.GetList(search, status, page, pageSize, sortBy, sortDirection);
            return Ok(result);
        }

        // ---------------------------------------------------------
        // POST /api/categories
        // ---------------------------------------------------------
        [HttpPost]
        public IActionResult Create([FromBody] CategoryCreateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Slug))
                return BadRequest(new { message = "validation_fill_all" });

            var id = _service.Create(req);
            return Ok(new { id });
        }

        // ---------------------------------------------------------
        // PUT /api/categories/{id}
        // ---------------------------------------------------------
        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] CategoryUpdateRequest req)
        {
            var success = _service.Update(id, req);
            if (!success)
                return NotFound(new { message = "not_found" });

            return Ok(new { id });
        }

        // ---------------------------------------------------------
        // PATCH /api/categories/{id}/status
        // ---------------------------------------------------------
        [HttpPatch("{id}/status")]
        public IActionResult ToggleStatus(long id)
        {
            var newStatus = _service.ToggleStatus(id);
            if (newStatus == null)
                return NotFound(new { message = "not_found" });

            return Ok(new { status = newStatus });
        }

        // ---------------------------------------------------------
        // DELETE /api/categories/{id}
        // ---------------------------------------------------------
        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            var success = _service.Delete(id);

            if (!success)
                return NotFound(new { message = "not_found" });

            return Ok(new { id });
        }
    }
}
