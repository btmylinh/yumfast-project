using Microsoft.Extensions.Configuration;
using Npgsql;

namespace WebApp.Services
{
    public class CategoryDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string Description { get; set; } = "";
        public short Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CategoryCreateRequest
    {
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Description { get; set; }
        public short Status { get; set; } = 1;
    }

    public class CategoryUpdateRequest
    {
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Description { get; set; }
        public short Status { get; set; } = 1;
    }

    public interface ICategoriesService
    {
        object GetList(string? search, int? status, int page, int pageSize, string? sortBy, string? sortDirection);
        long Create(CategoryCreateRequest req);
        bool Update(long id, CategoryUpdateRequest req);
        short? ToggleStatus(long id);
        bool Delete(long id);
    }

    public class CategoriesService : ICategoriesService
    {
        private readonly IConfiguration _config;

        public CategoriesService(IConfiguration config)
        {
            _config = config;
        }

        // -------------------------------------------------------------
        // Hàm lấy danh sách category (lọc + phân trang + sắp xếp)
        // -------------------------------------------------------------
        public object GetList(string? search, int? status, int page, int pageSize, string? sortBy, string? sortDirection)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var where = "WHERE 1=1";

            // Lọc theo search
            if (!string.IsNullOrWhiteSpace(search))
                where += " AND (name ILIKE @s OR slug ILIKE @s)";

            // Lọc theo status
            if (status.HasValue)
                where += " AND status=@st";

            // Xác định trường sắp xếp
            var sort = (sortBy?.ToLower()) switch
            {
                "name" => "name",
                "status" => "status",
                _ => "created_at"
            };

            // Hướng sắp xếp
            var dir = sortDirection?.ToLower() == "asc" ? "ASC" : "DESC";

            // Lấy tổng số bản ghi
            using var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM categories {where}", conn);

            if (!string.IsNullOrWhiteSpace(search)) countCmd.Parameters.AddWithValue("@s", $"%{search}%");
            if (status.HasValue) countCmd.Parameters.AddWithValue("@st", status.Value);

            var totalCount = (long)countCmd.ExecuteScalar();
            var offset = (page - 1) * pageSize;

            // Lấy danh sách
            using var cmd = new NpgsqlCommand($@"
                SELECT id, name, slug, description, status, created_at, updated_at
                FROM categories
                {where}
                ORDER BY {sort} {dir}
                LIMIT @ps OFFSET @off", conn);

            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@s", $"%{search}%");
            if (status.HasValue) cmd.Parameters.AddWithValue("@st", status.Value);

            cmd.Parameters.AddWithValue("@ps", pageSize);
            cmd.Parameters.AddWithValue("@off", offset);

            var list = new List<CategoryDto>();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new CategoryDto
                {
                    Id = reader.GetInt64(0),
                    Name = reader.GetString(1),
                    Slug = reader.GetString(2),
                    Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Status = reader.GetInt16(4),
                    CreatedAt = reader.GetDateTime(5),
                    UpdatedAt = reader.GetDateTime(6)
                });
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new { data = list, totalCount, totalPages };
        }

        // -------------------------------------------------------------
        // Hàm tạo mới category
        // -------------------------------------------------------------
        public long Create(CategoryCreateRequest req)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                INSERT INTO categories(name, slug, description, status)
                VALUES(@n, @sl, @d, @st)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("@n", req.Name);
            cmd.Parameters.AddWithValue("@sl", req.Slug);
            cmd.Parameters.AddWithValue("@d", (object?)req.Description ?? "");
            cmd.Parameters.AddWithValue("@st", req.Status);

            return (long)cmd.ExecuteScalar();
        }

        // -------------------------------------------------------------
        // Hàm cập nhật category
        // -------------------------------------------------------------
        public bool Update(long id, CategoryUpdateRequest req)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                UPDATE categories
                SET name=@n, slug=@sl, description=@d, status=@st
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("@n", req.Name);
            cmd.Parameters.AddWithValue("@sl", req.Slug);
            cmd.Parameters.AddWithValue("@d", (object?)req.Description ?? "");
            cmd.Parameters.AddWithValue("@st", req.Status);
            cmd.Parameters.AddWithValue("@id", id);

            return cmd.ExecuteNonQuery() > 0;
        }

        // -------------------------------------------------------------
        // Hàm bật/tắt trạng thái category
        // -------------------------------------------------------------
        public short? ToggleStatus(long id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var get = new NpgsqlCommand("SELECT status FROM categories WHERE id=@id", conn);
            get.Parameters.AddWithValue("@id", id);

            var cur = get.ExecuteScalar();
            if (cur == null)
                return null;

            var next = ((short)cur) == 1 ? (short)0 : (short)1;

            using var upd = new NpgsqlCommand("UPDATE categories SET status=@st WHERE id=@id", conn);
            upd.Parameters.AddWithValue("@st", next);
            upd.Parameters.AddWithValue("@id", id);
            upd.ExecuteNonQuery();

            return next;
        }

        // -------------------------------------------------------------
        // Hàm xóa category
        // -------------------------------------------------------------
        public bool Delete(long id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand("DELETE FROM categories WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            return cmd.ExecuteNonQuery() > 0;
        }
    }
}
