using Microsoft.Extensions.Configuration;
using Npgsql;
using WebApp.Models;
using System.Text;

namespace WebApp.Services
{
    public interface IOrdersService
    {
        object GetOrders(string? search, int? status, int page, int pageSize, string? sortBy, string? sortDirection,
            string? paymentMethod, DateTime? fromDate, DateTime? toDate, int? minPrice, int? maxPrice);
        object GetStatistics();
        byte[] ExportOrders(string? search, int? status, string? paymentMethod, DateTime? fromDate, DateTime? toDate, int? minPrice, int? maxPrice, string format = "excel");
    }

    public class OrdersService : IOrdersService
    {
        private readonly IConfiguration _config;

        public OrdersService(IConfiguration config)
        {
            _config = config;
        }

        // ---------------------------------------------------------
        // Lấy danh sách đơn hàng: lọc, tìm kiếm, phân trang
        // ---------------------------------------------------------
        public object GetOrders(string? search, int? status, int page, int pageSize, string? sortBy, string? sortDirection,
            string? paymentMethod, DateTime? fromDate, DateTime? toDate, int? minPrice, int? maxPrice)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            // Build JOIN clause for payment method filter
            var joinClause = "LEFT JOIN users u ON u.id=o.user_id";
            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                joinClause += " LEFT JOIN payment_transactions pt ON pt.order_id=o.id";
            }

            var where = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(search))
                where += " AND (o.code ILIKE @s OR o.ship_phone ILIKE @s OR u.email ILIKE @s)";

            if (status.HasValue)
                where += " AND o.status=@st";

            // Payment method filter - filter by payment_method from payment_transactions
            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                where += " AND pt.payment_method=@pm";
            }

            // Date range filter
            if (fromDate.HasValue)
                where += " AND o.created_at >= @fromDate";
            
            if (toDate.HasValue)
                where += " AND o.created_at <= @toDate";

            // Price range filter
            if (minPrice.HasValue)
                where += " AND o.total_price >= @minPrice";
            
            if (maxPrice.HasValue)
                where += " AND o.total_price <= @maxPrice";

            var sort = (sortBy?.ToLower()) switch
            {
                "code" => "o.code",
                "total_price" => "o.total_price",
                "status" => "o.status",
                _ => "o.created_at"
            };

            var dir = sortDirection?.ToLower() == "asc" ? "ASC" : "DESC";

            using var countCmd = new NpgsqlCommand(
                $"SELECT COUNT(DISTINCT o.id) FROM orders o {joinClause} {where}",
                conn
            );

            if (!string.IsNullOrWhiteSpace(search)) countCmd.Parameters.AddWithValue("@s", $"%{search}%");
            if (status.HasValue) countCmd.Parameters.AddWithValue("@st", status.Value);
            if (!string.IsNullOrWhiteSpace(paymentMethod)) countCmd.Parameters.AddWithValue("@pm", paymentMethod);
            if (fromDate.HasValue) countCmd.Parameters.AddWithValue("@fromDate", fromDate.Value.Date);
            if (toDate.HasValue) countCmd.Parameters.AddWithValue("@toDate", toDate.Value.Date.AddDays(1).AddTicks(-1)); // End of day
            if (minPrice.HasValue) countCmd.Parameters.AddWithValue("@minPrice", minPrice.Value);
            if (maxPrice.HasValue) countCmd.Parameters.AddWithValue("@maxPrice", maxPrice.Value);

            var total = (long)(countCmd.ExecuteScalar() ?? 0L);

            var offset = (page - 1) * pageSize;

            using var cmd = new NpgsqlCommand($@"
                SELECT DISTINCT
                    o.id, o.code, o.user_id, COALESCE(u.email,''), 
                    o.ship_phone, o.total_price, o.payment_status, 
                    o.status, o.created_at
                FROM orders o 
                {joinClause}
                {where}
                ORDER BY {sort} {dir}
                LIMIT @ps OFFSET @off", conn);

            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@s", $"%{search}%");
            if (status.HasValue) cmd.Parameters.AddWithValue("@st", status.Value);
            if (!string.IsNullOrWhiteSpace(paymentMethod)) cmd.Parameters.AddWithValue("@pm", paymentMethod);
            if (fromDate.HasValue) cmd.Parameters.AddWithValue("@fromDate", fromDate.Value.Date);
            if (toDate.HasValue) cmd.Parameters.AddWithValue("@toDate", toDate.Value.Date.AddDays(1).AddTicks(-1)); // End of day
            if (minPrice.HasValue) cmd.Parameters.AddWithValue("@minPrice", minPrice.Value);
            if (maxPrice.HasValue) cmd.Parameters.AddWithValue("@maxPrice", maxPrice.Value);

            cmd.Parameters.AddWithValue("@ps", pageSize);
            cmd.Parameters.AddWithValue("@off", offset);

            var list = new List<OrderDto>();

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new OrderDto
                {
                    Id = r.GetInt64(0),
                    Code = r.GetString(1),
                    UserId = r.IsDBNull(2) ? null : r.GetInt64(2),
                    Email = r.GetString(3),
                    Phone = r.GetString(4),
                    TotalPrice = r.GetInt32(5),
                    PaymentStatus = r.GetInt16(6),
                    Status = r.GetInt16(7),
                    CreatedAt = r.GetDateTime(8)
                });
            }

            return new { data = list, total };
        }

        // ---------------------------------------------------------
        // Lấy thống kê đơn hàng
        // ---------------------------------------------------------
        public object GetStatistics()
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            // Tổng số đơn hàng
            using var totalCmd = new NpgsqlCommand("SELECT COUNT(*) FROM orders", conn);
            var total = (long)(totalCmd.ExecuteScalar() ?? 0L);

            // Đơn hàng mới (Pending - status = 0)
            using var newCmd = new NpgsqlCommand("SELECT COUNT(*) FROM orders WHERE status = 0", conn);
            var newOrders = (long)(newCmd.ExecuteScalar() ?? 0L);

            // Đơn hàng đang xử lý (Processing - status = 1)
            using var processingCmd = new NpgsqlCommand("SELECT COUNT(*) FROM orders WHERE status = 1", conn);
            var processing = (long)(processingCmd.ExecuteScalar() ?? 0L);

            // Đơn hàng đã giao (Delivered - status = 3)
            using var deliveredCmd = new NpgsqlCommand("SELECT COUNT(*) FROM orders WHERE status = 3", conn);
            var delivered = (long)(deliveredCmd.ExecuteScalar() ?? 0L);

            // Đơn hàng đã hủy (Cancelled - status = 4)
            using var cancelledCmd = new NpgsqlCommand("SELECT COUNT(*) FROM orders WHERE status = 4", conn);
            var cancelled = (long)(cancelledCmd.ExecuteScalar() ?? 0L);

            // Tổng doanh thu (từ các đơn đã giao)
            using var revenueCmd = new NpgsqlCommand("SELECT COALESCE(SUM(total_price), 0) FROM orders WHERE status = 3", conn);
            var revenue = (long)(revenueCmd.ExecuteScalar() ?? 0L);

            // Đơn hàng hôm nay
            using var todayCmd = new NpgsqlCommand("SELECT COUNT(*) FROM orders WHERE DATE(created_at) = CURRENT_DATE", conn);
            var todayOrders = (long)(todayCmd.ExecuteScalar() ?? 0L);

            return new
            {
                total,
                newOrders,
                processing,
                delivered,
                cancelled,
                revenue,
                todayOrders
            };
        }

        // ---------------------------------------------------------
        // Xuất danh sách đơn hàng ra file Excel/CSV
        // ---------------------------------------------------------
        public byte[] ExportOrders(string? search, int? status, string? paymentMethod, DateTime? fromDate, DateTime? toDate, int? minPrice, int? maxPrice, string format = "excel")
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            // Build JOIN clause for payment method filter
            var joinClause = "LEFT JOIN users u ON u.id=o.user_id";
            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                joinClause += " LEFT JOIN payment_transactions pt ON pt.order_id=o.id";
            }

            var where = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(search))
                where += " AND (o.code ILIKE @s OR o.ship_phone ILIKE @s OR u.email ILIKE @s)";

            if (status.HasValue)
                where += " AND o.status=@st";

            if (!string.IsNullOrWhiteSpace(paymentMethod))
                where += " AND pt.payment_method=@pm";

            if (fromDate.HasValue)
                where += " AND o.created_at >= @fromDate";

            if (toDate.HasValue)
                where += " AND o.created_at <= @toDate";

            if (minPrice.HasValue)
                where += " AND o.total_price >= @minPrice";

            if (maxPrice.HasValue)
                where += " AND o.total_price <= @maxPrice";

            using var cmd = new NpgsqlCommand($@"
                SELECT DISTINCT
                    o.id, o.code, COALESCE(u.email,''), o.ship_phone, 
                    o.total_price, o.payment_status, o.status, o.created_at,
                    o.ship_name, o.ship_address_text
                FROM orders o 
                {joinClause}
                {where}
                ORDER BY o.created_at DESC", conn);

            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@s", $"%{search}%");
            if (status.HasValue) cmd.Parameters.AddWithValue("@st", status.Value);
            if (!string.IsNullOrWhiteSpace(paymentMethod)) cmd.Parameters.AddWithValue("@pm", paymentMethod);
            if (fromDate.HasValue) cmd.Parameters.AddWithValue("@fromDate", fromDate.Value.Date);
            if (toDate.HasValue) cmd.Parameters.AddWithValue("@toDate", toDate.Value.Date.AddDays(1).AddTicks(-1));
            if (minPrice.HasValue) cmd.Parameters.AddWithValue("@minPrice", minPrice.Value);
            if (maxPrice.HasValue) cmd.Parameters.AddWithValue("@maxPrice", maxPrice.Value);

            var orders = new List<OrderDto>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                orders.Add(new OrderDto
                {
                    Id = r.GetInt64(0),
                    Code = r.GetString(1),
                    Email = r.GetString(2),
                    Phone = r.GetString(3),
                    TotalPrice = r.GetInt32(4),
                    PaymentStatus = r.GetInt16(5),
                    Status = r.GetInt16(6),
                    CreatedAt = r.GetDateTime(7)
                });
            }

            // For now, both excel and csv use CSV format
            // Excel format can be enabled after EPPlus license setup
            return ExportToCsv(orders);
        }

        private byte[] ExportToExcel(List<OrderDto> orders)
        {
            // Use CSV format instead of Excel for now (EPPlus requires license setup)
            // Can be enhanced later with proper EPPlus license configuration
            return ExportToCsv(orders);
            
            // TODO: Uncomment when EPPlus is properly configured
            /*
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Orders");

            // Headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Mã đơn";
            worksheet.Cells[1, 3].Value = "Email";
            worksheet.Cells[1, 4].Value = "Số điện thoại";
            worksheet.Cells[1, 5].Value = "Tổng tiền";
            worksheet.Cells[1, 6].Value = "Trạng thái thanh toán";
            worksheet.Cells[1, 7].Value = "Trạng thái đơn";
            worksheet.Cells[1, 8].Value = "Ngày tạo";

            // Style header
            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Data
            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                var row = i + 2;
                worksheet.Cells[row, 1].Value = order.Id;
                worksheet.Cells[row, 2].Value = order.Code;
                worksheet.Cells[row, 3].Value = order.Email;
                worksheet.Cells[row, 4].Value = order.Phone;
                worksheet.Cells[row, 5].Value = order.TotalPrice;
                worksheet.Cells[row, 6].Value = GetPaymentStatusText(order.PaymentStatus);
                worksheet.Cells[row, 7].Value = GetOrderStatusText(order.Status);
                worksheet.Cells[row, 8].Value = order.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            }

            // Auto fit columns
            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
            */
        }

        private byte[] ExportToCsv(List<OrderDto> orders)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID,Mã đơn,Email,Số điện thoại,Tổng tiền,Trạng thái thanh toán,Trạng thái đơn,Ngày tạo");

            foreach (var order in orders)
            {
                csv.AppendLine($"{order.Id},{order.Code},{order.Email},{order.Phone},{order.TotalPrice},{GetPaymentStatusText(order.PaymentStatus)},{GetOrderStatusText(order.Status)},{order.CreatedAt:dd/MM/yyyy HH:mm}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        private string GetPaymentStatusText(short status)
        {
            return status switch
            {
                1 => "Đã thanh toán",
                2 => "Đã hoàn tiền",
                _ => "Chưa thanh toán"
            };
        }

        private string GetOrderStatusText(short status)
        {
            return status switch
            {
                0 => "Pending",
                1 => "Processing",
                2 => "Shipped",
                3 => "Delivered",
                4 => "Cancelled",
                _ => "Unknown"
            };
        }
    }
}
