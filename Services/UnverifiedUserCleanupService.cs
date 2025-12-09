using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace WebApp.Services
{
    public class UnverifiedUserCleanupService : BackgroundService
    {
        private readonly IConfiguration _config;
        public UnverifiedUserCleanupService(IConfiguration config)
        {
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
                    await conn.OpenAsync(stoppingToken);
                    var hours = int.TryParse(_config["Cleanup:UnverifiedUserRetentionHours"], out var h) ? h : 48;
                    using var cmd = new NpgsqlCommand($"DELETE FROM users WHERE status=0 AND created_at < NOW() - INTERVAL '{hours} hours'", conn);
                    await cmd.ExecuteNonQueryAsync(stoppingToken);
                }
                catch { }

                try
                {
                    var period = int.TryParse(_config["Cleanup:RunEveryHours"], out var p) ? p : 6;
                    await Task.Delay(TimeSpan.FromHours(period), stoppingToken);
                }
                catch { }
            }
        }
    }
}
